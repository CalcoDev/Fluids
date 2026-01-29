using Godot;

namespace Calcopod.Visualisation.Commands;

public class ArcCommand : DrawCommand
{
    public Vector2 Center { get; set; }
    public float Radius { get; set; }
    public float StartAngle { get; set; }
    public float EndAngle { get; set; }

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();
        float animEndAngle = Mathf.Lerp(StartAngle, EndAngle, t);
        var antiAlias = AntiAlias == AntiAlias.Enabled;

        int pointCount = Mathf.Max(4, (int)(Mathf.Abs(animEndAngle - StartAngle) / Mathf.Tau * 64));

        canvas.DrawArc(Center, Radius, StartAngle, animEndAngle, pointCount, Color, Thickness, antiAlias);

        // Draw rounded caps at arc endpoints if needed
        if (LineCaps == LineCaps.Round && Thickness > 0)
        {
            float radius = Thickness / 2f;
            Vector2 startPoint = Center + new Vector2(Mathf.Cos(StartAngle), Mathf.Sin(StartAngle)) * Radius;
            Vector2 endPoint = Center + new Vector2(Mathf.Cos(animEndAngle), Mathf.Sin(animEndAngle)) * Radius;
            canvas.DrawCircle(startPoint, radius, Color, antiAlias);
            canvas.DrawCircle(endPoint, radius, Color, antiAlias);
        }
    }
}