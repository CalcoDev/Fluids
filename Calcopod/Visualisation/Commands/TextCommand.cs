using Godot;

namespace Calcopod.Visualisation.Commands;

public class TextCommand : DrawCommand
{
    public Vector2 Position { get; set; }
    public string Text { get; set; } = "";
    public DrawFontKind FontKind { get; set; }
    public float FontSize { get; set; } = 14f;

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();

        // Animate by revealing characters
        int charCount = (int)(Text.Length * t);
        string displayText = Text[..charCount];

        if (string.IsNullOrEmpty(displayText)) return;

        Font font = component.GetFont(FontKind);
        if (font == null) return;

        canvas.DrawString(font, Position, displayText, HorizontalAlignment.Left, -1, (int)FontSize, Color);
    }
}