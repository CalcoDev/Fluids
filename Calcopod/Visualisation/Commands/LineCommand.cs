using Godot;

namespace Calcopod.Visualisation.Commands;

public class LineCommand : DrawCommand
{
    public Vector2 From { get; set; }
    public Vector2 To { get; set; }

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();
        Vector2 end = From.Lerp(To, t);
        canvas.DrawLine(From, end, Color, Thickness);
    }
}