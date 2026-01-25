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

        canvas.DrawLine(Center - new Vector2(halfSize, 0), Center + new Vector2(halfSize, 0), Color, Thickness);
        canvas.DrawLine(Center - new Vector2(0, halfSize), Center + new Vector2(0, halfSize), Color, Thickness);
    }
}