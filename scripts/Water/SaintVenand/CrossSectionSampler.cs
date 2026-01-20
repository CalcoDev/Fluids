using Godot;
using System;
using System.Collections.Generic;

namespace Fluids.Scripts.Water.SaintVenand;

public class CrossSectionSampler
{
    public struct CrossSectionProperties
    {
        public float BedElevation;
        public float Area;
        public float WettedPerimeter;
        public float TopWidth;
        public float HydraulicRadius;
    }

    private readonly World3D _world;
    private readonly Vector3 _channelAxis;
    private readonly Vector3 _widthAxis;
    private readonly Vector3 _upAxis;
    private readonly float _maxRayDistance;

    private readonly int _widthSamples;
    private readonly int _depthSamples;
    private readonly float _minDepth;

    public CrossSectionSampler(World3D world,
        Vector3 channelAxis, Vector3 widthAxis, Vector3 upAxis,
        float maxRayDistance,
        int widthSamples = 32, int depthSamples = 16, float minDepth = 1e-4f)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _channelAxis = channelAxis.Normalized();
        _widthAxis = widthAxis.Normalized();
        _upAxis = upAxis.Normalized();
        _maxRayDistance = maxRayDistance;
        _widthSamples = Math.Max(2, widthSamples);
        _depthSamples = Math.Max(2, depthSamples);
        _minDepth = minDepth;
    }

    public CrossSectionProperties SampleCrossSection(
        Vector3 centerPosition,
        float waterDepth,
        float channelWidth)
    {
        if (waterDepth < _minDepth)
        {
            return new CrossSectionProperties
            {
                BedElevation = centerPosition.Y,
                Area = 0.0f,
                WettedPerimeter = 0.0f,
                TopWidth = 0.0f,
                HydraulicRadius = 0.0f
            };
        }

        float bedElevation = FindBedElevation(centerPosition);

        var crossSectionPoints = new List<Vector2>();
        for (int d = 0; d < _depthSamples; d++)
        {
            float depthFraction = (float)d / (_depthSamples - 1);
            float currentElevation = bedElevation + waterDepth * depthFraction;

            // Sample width at this depth
            var widthPoints = SampleWidthAtDepth(centerPosition, currentElevation, channelWidth);

            foreach (var point in widthPoints)
            {
                crossSectionPoints.Add(point);
            }
        }

        return CalculateProperties(crossSectionPoints, bedElevation, channelWidth);
    }

    private float FindBedElevation(Vector3 position)
    {
        // Attempt raycasting if world is available
        if (_world?.DirectSpaceState != null)
        {
            try
            {
                var ray = PhysicsRayQueryParameters3D.Create(
                    position + _upAxis * _maxRayDistance,
                    position - _upAxis * _maxRayDistance
                );
                var result = _world.DirectSpaceState.IntersectRay(ray);

                if (result != null && result.Count > 0)
                {
                    return ((Vector3)result["position"]).Y;
                }
            }
            catch
            {
                // Fallback to flat bed
            }
        }

        // Assume bed is flat if world doesn't exist lmfao
        return position.Y;
    }

    private List<Vector2> SampleWidthAtDepth(Vector3 centerPosition, float elevation, float channelWidth)
    {
        var points = new List<Vector2>();

        // Fallback to assumption of rectangular bed
        if (_world?.DirectSpaceState == null)
        {
            return EstimateWidthAtDepth(channelWidth);
        }

        float maxWidth = channelWidth * 1.5f;

        // Find channel boudnaries
        for (int i = 0; i < _widthSamples; i++)
        {
            float widthFraction = (i / (float)(_widthSamples - 1)) - 0.5f;
            float rayOffset = widthFraction * maxWidth;

            Vector3 rayStart = centerPosition + _widthAxis * rayOffset;
            rayStart.Y = elevation;

            try
            {
                var ray = PhysicsRayQueryParameters3D.Create(
                    rayStart + _channelAxis * _maxRayDistance,
                    rayStart - _channelAxis * _maxRayDistance
                );
                var result = _world.DirectSpaceState.IntersectRay(ray);

                if (result != null && result.Count > 0)
                {
                    Vector3 hitPoint = (Vector3)result["position"];
                    float distanceFromCenter = rayOffset;
                    float depthFromSurface = hitPoint.Y - elevation;
                    points.Add(new Vector2(distanceFromCenter, depthFromSurface));
                }
            }
            catch
            {
                // Continue to next ray if this one fails
            }
        }

        // Fallback to assumption of rectangular bed
        if (points.Count < 2)
        {
            points = EstimateWidthAtDepth(channelWidth);
        }

        return points;
    }

    private List<Vector2> EstimateWidthAtDepth(float channelWidth)
    {
        var points = new List<Vector2>();

        // Sample across the channel width assuming rectangular or simple trapezoidal shape
        // Assume channel sides are vertical
        for (int i = 0; i < _widthSamples; i++)
        {
            float widthFraction = (i / (float)(_widthSamples - 1)) - 0.5f;  // -0.5 to 0.5
            float rayOffset = widthFraction * channelWidth;
            float depthFromSurface = 0.0f;
            points.Add(new Vector2(rayOffset, depthFromSurface));
        }

        return points;
    }

    private static CrossSectionProperties CalculateProperties(List<Vector2> points,
        float bedElevation, float channelWidth)
    {
        if (points.Count < 2)
        {
            return new CrossSectionProperties
            {
                BedElevation = bedElevation,
                Area = 0.0f,
                WettedPerimeter = 0.0f,
                TopWidth = 0.0f,
                HydraulicRadius = 0.0f
            };
        }

        points.Sort((a, b) => a.X.CompareTo(b.X));

        // Trapezoid rule
        float area = 0.0f;
        float wettedPerimeter = 0.0f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];

            float dWidth = p2.X - p1.X;
            float dDepthAvg = (p1.Y + p2.Y) * 0.5f;

            // Trapezoidal area integration
            area += dWidth * dDepthAvg;

            // Distance along wetted boundary (simplified)
            float ds = MathF.Sqrt(dWidth * dWidth + (p2.Y - p1.Y) * (p2.Y - p1.Y));
            wettedPerimeter += ds;
        }

        // Add bed segment length (width of channel at bed level)
        Vector2 leftPoint = points[0];
        Vector2 rightPoint = points[^1];
        float bedWidth = rightPoint.X - leftPoint.X;
        wettedPerimeter += bedWidth;

        // Add side slopes to wetted perimeter
        if (leftPoint.Y > 0)
        {
            float v = channelWidth * 0.5f;
            float sideLength = MathF.Sqrt(v * v + leftPoint.Y * leftPoint.Y);
            wettedPerimeter += sideLength;
        }

        if (rightPoint.Y > 0)
        {
            float v = channelWidth * 0.5f;
            float sideLength = MathF.Sqrt(v * v + rightPoint.Y * rightPoint.Y);
            wettedPerimeter += sideLength;
        }

        float topWidth = bedWidth;
        float hydraulicRadius = wettedPerimeter > 0 ? area / wettedPerimeter : 0.0f;

        return new CrossSectionProperties
        {
            BedElevation = bedElevation,
            Area = MathF.Max(area, 0.0f),
            WettedPerimeter = MathF.Max(wettedPerimeter, 0.0f),
            TopWidth = MathF.Max(topWidth, 0.0f),
            HydraulicRadius = hydraulicRadius
        };
    }
}
