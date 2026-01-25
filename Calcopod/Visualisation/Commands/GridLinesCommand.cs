using Godot;

namespace Calcopod.Visualisation.Commands;

public class GridLinesCommand : DrawCommand
{
    public Rect2 Area { get; set; }
    public Vector2 CellSize { get; set; }

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();

        // Calculate number of lines
        int hLines = (int)(Area.Size.Y / CellSize.Y) + 1;
        int vLines = (int)(Area.Size.X / CellSize.X) + 1;

        int totalLines = hLines + vLines;
        int linesToDraw = (int)(totalLines * t);

        int linesDrawn = 0;

        // Draw horizontal lines
        for (int i = 0; i <= hLines && linesDrawn < linesToDraw; i++, linesDrawn++)
        {
            float y = Area.Position.Y + i * CellSize.Y;
            if (y > Area.Position.Y + Area.Size.Y) break;
            canvas.DrawLine(
                new Vector2(Area.Position.X, y),
                new Vector2(Area.Position.X + Area.Size.X, y),
                Color, Thickness);
        }

        // Draw vertical lines
        for (int i = 0; i <= vLines && linesDrawn < linesToDraw; i++, linesDrawn++)
        {
            float x = Area.Position.X + i * CellSize.X;
            if (x > Area.Position.X + Area.Size.X) break;
            canvas.DrawLine(
                new Vector2(x, Area.Position.Y),
                new Vector2(x, Area.Position.Y + Area.Size.Y),
                Color, Thickness);
        }
    }
}