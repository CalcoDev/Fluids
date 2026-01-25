using Godot;
using System.Collections.Generic;

namespace Calcopod.Visualisation.Commands;

public class PolylineCommand : DrawCommand
{
    public List<Vector2> Points { get; set; } = new();
    public bool Closed { get; set; }

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        if (Points == null || Points.Count < 2) return;

        float t = GetAnimT();
        var pointsToDraw = GetAnimatedPoints(t);

        if (pointsToDraw.Length < 2) return;

        if (Closed && pointsToDraw.Length == Points.Count)
        {
            // Draw as closed polygon outline
            var closedPoints = new Vector2[pointsToDraw.Length + 1];
            pointsToDraw.CopyTo(closedPoints, 0);
            closedPoints[^1] = pointsToDraw[0];
            canvas.DrawPolyline(closedPoints, Color, Thickness);
        }
        else
        {
            canvas.DrawPolyline(pointsToDraw, Color, Thickness);
        }
    }

    private Vector2[] GetAnimatedPoints(float t)
    {
        if (t >= 1f) return Points.ToArray();

        // Calculate total arc length
        float totalLength = 0f;
        for (int i = 1; i < Points.Count; i++)
        {
            totalLength += Points[i - 1].DistanceTo(Points[i]);
        }

        float targetLength = totalLength * t;
        float currentLength = 0f;

        var result = new List<Vector2> { Points[0] };

        for (int i = 1; i < Points.Count; i++)
        {
            float segmentLength = Points[i - 1].DistanceTo(Points[i]);
            if (currentLength + segmentLength >= targetLength)
            {
                // Partial segment
                float remaining = targetLength - currentLength;
                float segT = segmentLength > 0 ? remaining / segmentLength : 0;
                result.Add(Points[i - 1].Lerp(Points[i], segT));
                break;
            }
            else
            {
                result.Add(Points[i]);
                currentLength += segmentLength;
            }
        }

        return result.ToArray();
    }
}