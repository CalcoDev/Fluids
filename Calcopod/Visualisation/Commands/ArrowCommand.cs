using Godot;

namespace Calcopod.Visualisation.Commands;

public class ArrowCommand : DrawCommand
{
    public Vector2 From { get; set; }
    public Vector2 To { get; set; }
    public float HeadSize { get; set; } = 10f;

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();
        Vector2 end = From.Lerp(To, t);

        var antiAlias = AntiAlias == AntiAlias.Enabled;

        // Draw shaft
        canvas.DrawLine(From, end, Color, Thickness, antiAlias);

        // Draw rounded caps if needed
        if (LineCaps == LineCaps.Round && Thickness > 0)
        {
            float radius = Thickness / 2f;
            canvas.DrawCircle(From, radius, Color, antiAlias);
            canvas.DrawCircle(end, radius, Color, antiAlias);
        }

        // Draw head only when animation is mostly complete (last 20%)
        if (t > 0.8f)
        {
            // 0 to 1 for head animation
            float headT = (t - 0.8f) / 0.2f;
            float animHeadSize = HeadSize * headT;

            Vector2 dir = (To - From).Normalized();
            Vector2 perp = new Vector2(-dir.Y, dir.X);

            Vector2 headBase = end - dir * animHeadSize;
            Vector2 left = headBase + perp * animHeadSize * 0.5f;
            Vector2 right = headBase - perp * animHeadSize * 0.5f;

            canvas.DrawLine(end, left, Color, Thickness, antiAlias);
            canvas.DrawLine(end, right, Color, Thickness, antiAlias);

            // Round caps on arrow head if needed
            if (LineCaps == LineCaps.Round && Thickness > 0)
            {
                float radius = Thickness / 2f;
                canvas.DrawCircle(left, radius, Color, antiAlias);
                canvas.DrawCircle(right, radius, Color, antiAlias);
            }
        }
    }
}