using Godot;

namespace Calcopod.Visualisation.Commands;

public class CircleCommand : DrawCommand
{
    public Vector2 Center { get; set; }
    public float Radius { get; set; }
    public bool Filled { get; set; }

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();
        float animRadius = Radius * t;
        var antiAlias = AntiAlias == AntiAlias.Enabled;

        if (Filled)
        {
            canvas.DrawCircle(Center, animRadius, Color, antiAlias);
        }
        else
        {
            canvas.DrawArc(Center, animRadius, 0, Mathf.Tau, 64, Color, Thickness, antiAlias);
        }
    }
}