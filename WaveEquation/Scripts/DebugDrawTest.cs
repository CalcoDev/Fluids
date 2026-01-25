#nullable enable
using Calcopod.Visualisation;
using Godot;

namespace Fluids.WaveEquation.Scripts;

/// <summary>
/// Comprehensive test of the DebugDraw2D system.
/// Tests basic shapes, animation, layering, coordinate spaces, fonts, and interactivity.
/// </summary>
public partial class DebugDrawTest : Node
{
    [Export] public Node2D? _node2D;

    private float _animationTime = 0f;
    private Vector2 _mousePos = Vector2.Zero;
    private bool _isMouseOverButton = false;

    public override void _Ready()
    {
        GD.Print("[DebugDrawTest] Initialized. Testing DebugDraw2D system.");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motionEvent)
        {
            _mousePos = motionEvent.Position;

            // Check if hovering over interactive button area
            _isMouseOverButton = new Rect2(400, 400, 150, 50).HasPoint(_mousePos);
        }
    }

    public override void _Process(double delta)
    {
        float deltaF = (float)delta;
        _animationTime += deltaF;

        // ==============================================================
        // WORLD SPACE TESTS
        // ==============================================================

        // Test 1: Basic Lines with different thicknesses
        Draw.Line(new Vector2(10, 10), new Vector2(100, 10), Colors.Red, thickness: 1f, layer: 0);
        Draw.Line(new Vector2(10, 30), new Vector2(100, 30), Colors.Red, thickness: 3f, layer: 0);
        Draw.Line(new Vector2(10, 50), new Vector2(100, 50), Colors.Red, thickness: 5f, layer: 0);
        Draw.Text(new Vector2(110, 10), "Lines (1, 3, 5px)", Colors.White, DrawFontKind.Mono, size: 12f);

        // Test 2: Animated Line (extends endpoint)
        float lineAnimT = Mathf.Sin(_animationTime * Mathf.Tau) * 0.5f + 0.5f;
        Draw.Line(
            new Vector2(10, 80),
            new Vector2(150, 80),
            new Color(1f, lineAnimT, 0f), // Orange to yellow
            thickness: 2f,
            animDuration: 2f,
            duration: 99f,
            layer: 1);
        Draw.Text(new Vector2(160, 75), "Animated endpoint", Colors.Yellow, DrawFontKind.Mono, size: 11f);

        // Test 3: Circles - filled vs outline, different sizes
        Draw.Circle(new Vector2(50, 130), 15f, Colors.Blue, filled: true, layer: 0);
        Draw.Circle(new Vector2(90, 130), 15f, Colors.Green, filled: false, thickness: 2f, layer: 0);
        Draw.Circle(new Vector2(130, 130), 15f, Colors.Magenta, filled: false, thickness: 3f, layer: 0);
        Draw.Text(new Vector2(160, 120), "Circles (filled, outline, thick)", Colors.White, DrawFontKind.Mono, size: 11f);

        // Test 4: Animated Circle (grows/shrinks)
        float circleRadius = 10f + Mathf.Sin(_animationTime * 2f) * 8f;
        Draw.Circle(
            new Vector2(50, 180),
            circleRadius,
            new Color(circleRadius / 20f, 0.5f, 1f),
            filled: false,
            thickness: 2f,
            layer: 0);
        Draw.Text(new Vector2(70, 170), "Animated radius", Colors.Cyan, DrawFontKind.Mono, size: 11f);

        // Test 5: Rectangles - filled vs outline
        Draw.Rect(new Rect2(10, 200, 50, 30), Colors.Purple, filled: true, layer: 0);
        Draw.Rect(new Rect2(70, 200, 50, 30), Colors.Purple, filled: false, thickness: 2f, layer: 0);
        Draw.Rect(new Rect2(130, 200, 50, 30), Colors.Purple, filled: false, thickness: 4f, layer: 0);
        Draw.Text(new Vector2(190, 205), "Rects", Colors.White, DrawFontKind.Mono, size: 11f);

        // Test 6: Polyline (no animation)
        var polylinePoints = new System.Collections.Generic.List<Vector2>
        {
            new Vector2(10, 260),
            new Vector2(50, 240),
            new Vector2(80, 270),
            new Vector2(120, 250)
        };
        Draw.Polyline(polylinePoints, Colors.Cyan, thickness: 2f, layer: 0);
        Draw.Text(new Vector2(130, 250), "Polyline", Colors.White, DrawFontKind.Mono, size: 11f);

        // Test 7: Arc/Partial Circle
        float arcStartAngle = _animationTime * 2f;
        float arcEndAngle = arcStartAngle + Mathf.Pi;
        Draw.Arc(new Vector2(50, 320), 20f, arcStartAngle, arcEndAngle, Colors.Orange, thickness: 2f, layer: 0);
        Draw.Text(new Vector2(80, 310), "Arc (animated)", Colors.Orange, DrawFontKind.Mono, size: 11f);

        // Test 8: Arrow
        Vector2 arrowFrom = new Vector2(10, 360);
        Vector2 arrowTo = new Vector2(100, 360);
        Draw.Arrow(arrowFrom, arrowTo, Colors.Lime, headSize: 8f, thickness: 2f, layer: 1);
        Draw.Text(new Vector2(110, 350), "Arrow", Colors.Lime, DrawFontKind.Mono, size: 11f);

        // Test 9: Cross marker
        Draw.Cross(new Vector2(150, 360), size: 20f, Colors.Red, thickness: 2f, layer: 0);
        Draw.Text(new Vector2(170, 350), "Cross", Colors.Red, DrawFontKind.Mono, size: 11f);

        // Test 10: Grid Lines
        Draw.GridLines(
            new Rect2(10, 400, 120, 80),
            new Vector2(20, 20),
            new Color(0.5f, 0.5f, 0.5f),
            thickness: 0.5f,
            layer: 0);
        Draw.Text(new Vector2(140, 400), "GridLines", Colors.Gray, DrawFontKind.Mono, size: 11f);

        // Test 11: Grid Squares (filled)
        Draw.GridSquares(
            new Rect2(150, 400, 80, 80),
            new Vector2(20, 20),
            Colors.Cyan,
            filled: true,
            layer: 0);
        Draw.Text(new Vector2(240, 400), "GridSquares", Colors.Cyan, DrawFontKind.Mono, size: 11f);

        // ==============================================================
        // LAYERING TEST
        // ==============================================================

        // Test 12: Draw on different layers (lower layers first visually)
        Draw.Circle(new Vector2(350, 50), 25f, Colors.Red, filled: true, layer: 0);
        Draw.Circle(new Vector2(370, 65), 25f, Colors.Green, filled: true, layer: 1);
        Draw.Circle(new Vector2(390, 50), 25f, Colors.Blue, filled: true, layer: 2);
        Draw.Text(new Vector2(330, 90), "Layers: 0,1,2 (back to front)", Colors.White, DrawFontKind.Mono, size: 11f);

        // ==============================================================
        // ANIMATION & TIMING TESTS
        // ==============================================================

        // Test 13: One-shot timed commands
        if (Mathf.Floor(_animationTime * 2) % 4 == 0)
        {
            Draw.Circle(
                new Vector2(350, 150 + Mathf.Sin(_animationTime) * 10f),
                5f,
                Colors.Yellow,
                filled: true,
                duration: 0.5f,
                layer: 0);
        }
        Draw.Text(new Vector2(330, 140), "Timed circles\n(0.5s each)", Colors.Yellow, DrawFontKind.Mono, size: 10f);

        // Test 14: Line with animation
        float lineTime = Mathf.PosMod(_animationTime, 3f);
        Draw.Line(
            new Vector2(330, 200),
            new Vector2(430, 200),
            new Color(lineTime / 3f, 1f - lineTime / 3f, 0.5f),
            thickness: 2f,
            animDuration: 3f,
            duration: 99f,
            layer: 1);
        Draw.Text(new Vector2(330, 220), "Line (3s anim)", Colors.White, DrawFontKind.Mono, size: 11f);

        // ==============================================================
        // FONT TESTS
        // ==============================================================

        // Test 15: All three font kinds
        Draw.Text(new Vector2(10, 510), "MonoFont: ABCD 123", Colors.White, DrawFontKind.Mono, size: 14f, layer: 0);
        Draw.Text(new Vector2(10, 535), "SerifFont: ABCD 123", Colors.LimeGreen, DrawFontKind.Serif, size: 14f, layer: 0);
        Draw.Text(new Vector2(10, 560), "PixelFont: ABCD 123", Colors.Yellow, DrawFontKind.Pixel, size: 14f, layer: 0);

        // ==============================================================
        // INTERACTIVE TEST (Screen Space)
        // ==============================================================

        // Test 16: Mouse position indicator (screen space)
        Draw.Text(
            _mousePos + new Vector2(10, -10),
            $"Mouse: {_mousePos.X:F0}, {_mousePos.Y:F0}",
            Colors.White,
            DrawFontKind.Mono,
            size: 12f,
            space: DrawSpace.Screen,
            layer: 10);

        // Test 17: Interactive button (changes color on hover)
        var buttonRect = new Rect2(400, 400, 150, 50);
        Color buttonColor = _isMouseOverButton ? Colors.LimeGreen : Colors.Red;
        Draw.Rect(buttonRect, buttonColor, filled: false, thickness: 3f, space: DrawSpace.Screen, layer: 9);
        Draw.Text(
            new Vector2(420, 415),
            _isMouseOverButton ? "HOVERING!" : "Hover Me!",
            buttonColor,
            DrawFontKind.Mono,
            size: 14f,
            space: DrawSpace.Screen,
            layer: 9);

        // Test 18: Animated progress bar (screen space)
        float progress = Mathf.PosMod(_animationTime, 3f) / 3f;
        Draw.Rect(
            new Rect2(350, 480, 200, 20),
            Colors.DarkGray,
            filled: false,
            thickness: 1f,
            space: DrawSpace.Screen,
            layer: 8);
        Draw.Rect(
            new Rect2(350, 480, 200 * progress, 20),
            Colors.Green,
            filled: true,
            space: DrawSpace.Screen,
            layer: 8);

        // Test 19: Screen space grid (subtle)
        Draw.GridLines(
            new Rect2(600, 350, 200, 150),
            new Vector2(25, 25),
            new Color(0.3f, 0.3f, 0.3f),
            thickness: 0.5f,
            space: DrawSpace.Screen,
            layer: 7);
        Draw.Text(
            new Vector2(610, 360),
            "Screen Space Grid",
            Colors.Gray,
            DrawFontKind.Mono,
            size: 12f,
            space: DrawSpace.Screen,
            layer: 7);

        // ==============================================================
        // INSERTION ORDER TEST (all same layer)
        // ==============================================================

        // Test 20: Verify insertion order - drawn in order
        Draw.Circle(new Vector2(700, 100), 8f, Colors.Red, filled: true, layer: 5);
        Draw.Circle(new Vector2(710, 100), 8f, Colors.Green, filled: true, layer: 5);
        Draw.Circle(new Vector2(720, 100), 8f, Colors.Blue, filled: true, layer: 5);
        Draw.Text(new Vector2(700, 120), "Insertion order\n(R->G->B)", Colors.White, DrawFontKind.Mono, size: 10f);

        // ==============================================================
        // COLOR GRADIENT TEST
        // ==============================================================

        // Test 21: Rainbow line spectrum
        for (int i = 0; i < 10; i++)
        {
            float hue = i / 10f;
            Color rainbowColor = Color.FromHsv(hue, 1f, 1f);
            Draw.Line(
                new Vector2(600 + i * 10, 200),
                new Vector2(600 + i * 10, 250),
                rainbowColor,
                thickness: 3f,
                layer: 0);
        }
        Draw.Text(new Vector2(600, 260), "Rainbow spectrum", Colors.White, DrawFontKind.Mono, size: 11f);

        // ==============================================================
        // SCOPE OVERRIDE TEST
        // ==============================================================

        // Test 22: Use scope override (if another component exists)
        Draw.Text(new Vector2(600, 300), "Scope Override (if active component exists)", Colors.White, DrawFontKind.Mono, size: 10f);

        // ==============================================================
        // HELP TEXT
        // ==============================================================

        Draw.Text(
            new Vector2(10, _node2D!.GetViewportRect().Size.Y - 20),
            "DebugDrawTest: Tests all DebugDraw2D features. Move mouse to test interactivity.",
            Colors.White,
            DrawFontKind.Mono,
            size: 11f,
            space: DrawSpace.Screen,
            layer: 100);
    }
}
