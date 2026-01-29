using Godot;

namespace Calcopod.Visualisation.Commands;

public class CrossCommand : DrawCommand
{
    public Vector2 Center { get; set; }
    public float Size { get; set; }

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();
        float halfSize = Size * 0.5f * t;

        Vector2 hEnd = Center + new Vector2(halfSize, 0);
        Vector2 hStart = Center - new Vector2(halfSize, 0);
        Vector2 vEnd = Center + new Vector2(0, halfSize);
        Vector2 vStart = Center - new Vector2(0, halfSize);

        var antiAlias = AntiAlias == AntiAlias.Enabled;

        canvas.DrawLine(hStart, hEnd, Color, Thickness, antiAlias);
        canvas.DrawLine(vStart, vEnd, Color, Thickness, antiAlias);

        // Draw rounded caps if needed
        if (LineCaps == LineCaps.Round && Thickness > 0)
        {
            float radius = Thickness / 2f;
            canvas.DrawCircle(hStart, radius, Color, antiAlias);
            canvas.DrawCircle(hEnd, radius, Color, antiAlias);
            canvas.DrawCircle(vStart, radius, Color, antiAlias);
            canvas.DrawCircle(vEnd, radius, Color, antiAlias);
        }
    }
}