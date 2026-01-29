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

        // Draw line with anti-aliasing if enabled
        var antiAlias = AntiAlias == AntiAlias.Enabled;
        canvas.DrawLine(From, end, Color, Thickness, antiAlias);

        // Draw rounded caps if needed
        if (LineCaps == LineCaps.Round && Thickness > 0)
        {
            float radius = Thickness / 2f;
            canvas.DrawCircle(From, radius, Color, antiAlias);
            canvas.DrawCircle(end, radius, Color, antiAlias);
        }
    }
}
