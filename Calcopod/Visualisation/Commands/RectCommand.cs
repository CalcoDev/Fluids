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

            canvas.DrawLine(topLeft, topRight, Color, Thickness);
            canvas.DrawLine(topRight, bottomRight, Color, Thickness);
            canvas.DrawLine(bottomRight, bottomLeft, Color, Thickness);
            canvas.DrawLine(bottomLeft, topLeft, Color, Thickness);
        }
    }
}