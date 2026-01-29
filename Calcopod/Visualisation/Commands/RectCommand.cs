using Godot;

namespace Calcopod.Visualisation.Commands;

public class RectCommand : DrawCommand
{
    public Rect2 Rect { get; set; }
    public bool Filled { get; set; }

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();

        // Animate from center outward
        Vector2 center = Rect.GetCenter();
        Vector2 halfSize = Rect.Size * 0.5f * t;
        Rect2 animRect = new Rect2(center - halfSize, halfSize * 2f);
        var antiAlias = AntiAlias == AntiAlias.Enabled;

        if (Filled)
        {
            canvas.DrawRect(animRect, Color);
        }
        else
        {
            var topLeft = animRect.Position;
            var topRight = animRect.Position + new Vector2(animRect.Size.X, 0);
            var bottomRight = animRect.Position + animRect.Size;
            var bottomLeft = animRect.Position + new Vector2(0, animRect.Size.Y);

            canvas.DrawLine(topLeft, topRight, Color, Thickness, antiAlias);
            canvas.DrawLine(topRight, bottomRight, Color, Thickness, antiAlias);
            canvas.DrawLine(bottomRight, bottomLeft, Color, Thickness, antiAlias);
            canvas.DrawLine(bottomLeft, topLeft, Color, Thickness, antiAlias);

            // Draw rounded corners if needed
            if (LineCaps == LineCaps.Round && Thickness > 0)
            {
                float radius = Thickness / 2f;
                canvas.DrawCircle(topLeft, radius, Color, antiAlias);
                canvas.DrawCircle(topRight, radius, Color, antiAlias);
                canvas.DrawCircle(bottomRight, radius, Color, antiAlias);
                canvas.DrawCircle(bottomLeft, radius, Color, antiAlias);
            }
        }
    }
}