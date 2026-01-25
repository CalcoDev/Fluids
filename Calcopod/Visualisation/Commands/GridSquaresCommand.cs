using Godot;

namespace Calcopod.Visualisation.Commands;

public class GridSquaresCommand : DrawCommand
{
    public Rect2 Area { get; set; }
    public Vector2 CellSize { get; set; }
    public bool Filled { get; set; }

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();

        int cols = (int)(Area.Size.X / CellSize.X);
        int rows = (int)(Area.Size.Y / CellSize.Y);
        int totalSquares = cols * rows;
        int squaresToDraw = (int)(totalSquares * t);

        int squaresDrawn = 0;

        for (int row = 0; row < rows && squaresDrawn < squaresToDraw; row++)
        {
            for (int col = 0; col < cols && squaresDrawn < squaresToDraw; col++, squaresDrawn++)
            {
                Vector2 pos = Area.Position + new Vector2(col * CellSize.X, row * CellSize.Y);
                Rect2 cellRect = new Rect2(pos, CellSize);

                if (Filled)
                {
                    canvas.DrawRect(cellRect, Color);
                }
                else
                {
                    var topLeft = cellRect.Position;
                    var topRight = cellRect.Position + new Vector2(cellRect.Size.X, 0);
                    var bottomRight = cellRect.Position + cellRect.Size;
                    var bottomLeft = cellRect.Position + new Vector2(0, cellRect.Size.Y);

                    canvas.DrawLine(topLeft, topRight, Color, Thickness);
                    canvas.DrawLine(topRight, bottomRight, Color, Thickness);
                    canvas.DrawLine(bottomRight, bottomLeft, Color, Thickness);
                    canvas.DrawLine(bottomLeft, topLeft, Color, Thickness);
                }
            }
        }
    }
}
