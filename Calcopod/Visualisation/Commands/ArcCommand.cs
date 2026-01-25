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

        int pointCount = Mathf.Max(4, (int)(Mathf.Abs(animEndAngle - StartAngle) / Mathf.Tau * 64));
        canvas.DrawArc(Center, Radius, StartAngle, animEndAngle, pointCount, Color, Thickness);
    }
}