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

    // Performance optimization: caching
    private Dictionary<int, CrossSectionProperties> _sectionCache;
    private Dictionary<int, float> _bedElevationCache;
    private float _lastCachedWaterDepth = -1f;
    private const float DEPTH_CHANGE_THRESHOLD = 0.001f;  // Invalidate cache if depth changes by more than this

    public CrossSectionSampler(World3D world,
        Vector3 channelAxis, Vector3 widthAxis, Vector3 upAxis,
        float maxRayDistance,
        int widthSamples = 32, int depthSamples = 16, float minDepth = 1e-4f)
    {
        _world = world;
        _channelAxis = channelAxis.Normalized();
        _widthAxis = widthAxis.Normalized();
        _upAxis = upAxis.Normalized();
        _maxRayDistance = maxRayDistance;
        _widthSamples = Math.Max(2, widthSamples);
        _depthSamples = Math.Max(2, depthSamples);
        _minDepth = minDepth;

        // Initialize caches
        _sectionCache = new Dictionary<int, CrossSectionProperties>();
        _bedElevationCache = new Dictionary<int, float>();
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

        // Generate cache key from position
        int posKey = GetPositionKey(centerPosition);

        // Check if we should invalidate cache due to depth change
        if (Math.Abs(waterDepth - _lastCachedWaterDepth) > DEPTH_CHANGE_THRESHOLD)
        {
            _sectionCache.Clear();
            _lastCachedWaterDepth = waterDepth;
        }

        // Check cache
        if (_sectionCache.TryGetValue(posKey, out var cached))
        {
            return cached;
        }

        // Not in cache - compute it
        float bedElevation = FindBedElevationCached(centerPosition);

        var crossSectionPoints = new List<Vector2>();

        // OPTIMIZATION: Use only 4 depth samples and 8 width samples
        int depthSamplesToUse = 4;
        int widthSamplesToUse = 8;

        for (int d = 0; d < depthSamplesToUse; d++)
        {
            float depthFraction = (float)d / (depthSamplesToUse - 1);
            float currentElevation = bedElevation + waterDepth * depthFraction;

            // Sample width at this depth
            var widthPoints = SampleWidthAtDepth(centerPosition, currentElevation, channelWidth, widthSamplesToUse);

            foreach (var point in widthPoints)
            {
                crossSectionPoints.Add(point);
            }
        }

        var result = CalculateProperties(crossSectionPoints, bedElevation, channelWidth);

        // Cache the result
        _sectionCache[posKey] = result;

        return result;
    }

    private int GetPositionKey(Vector3 position)
    {
        // Quantize position to reduce cache misses from slightly different positions
        const float QUANTIZE = 1.0f;
        int x = (int)(position.X / QUANTIZE);
        int z = (int)(position.Z / QUANTIZE);
        return x * 73856093 ^ z * 19349663;
    }

    private float FindBedElevationCached(Vector3 position)
    {
        int posKey = GetPositionKey(position);

        if (_bedElevationCache.TryGetValue(posKey, out var cached))
        {
            return cached;
        }

        float bedElev = FindBedElevation(position);
        _bedElevationCache[posKey] = bedElev;
        return bedElev;
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

    private List<Vector2> SampleWidthAtDepth(Vector3 centerPosition, float elevation, float channelWidth, int widthSamplesToUse)
    {
        var points = new List<Vector2>();

        // Fallback to assumption of rectangular bed
        if (_world?.DirectSpaceState == null)
        {
            return EstimateWidthAtDepth(channelWidth, widthSamplesToUse);
        }

        float maxWidth = channelWidth * 1.5f;

        // Find channel boundaries - use reduced sample count
        for (int i = 0; i < widthSamplesToUse; i++)
        {
            float widthFraction = (i / (float)(widthSamplesToUse - 1)) - 0.5f;
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
            points = EstimateWidthAtDepth(channelWidth, widthSamplesToUse);
        }

        return points;
    }

    private List<Vector2> EstimateWidthAtDepth(float channelWidth, int widthSamplesToUse)
    {
        var points = new List<Vector2>();

        // Sample across the channel width assuming rectangular shape
        for (int i = 0; i < widthSamplesToUse; i++)
        {
            float widthFraction = (i / (float)(widthSamplesToUse - 1)) - 0.5f;
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
