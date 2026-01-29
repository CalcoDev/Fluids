#nullable enable
using Godot;
using System.Collections.Generic;
using Calcopod.Visualisation.Commands;

namespace Calcopod.Visualisation;

/// <summary>
/// Static facade for debug drawing. All gameplay code should use this API.
/// </summary>
public static class Draw
{
    #region Target Stack

    private static readonly Stack<DebugDrawComponent> _targetStack = new();

    /// <summary>
    /// Gets the current target component based on targeting rules.
    /// </summary>
    public static DebugDrawComponent? CurrentTarget
    {
        get
        {
            // 1. Stack top
            if (_targetStack.Count > 0)
            {
                var top = _targetStack.Peek();
                if (GodotObject.IsInstanceValid(top))
                    return top;
                // Invalid component on stack, pop it
                _targetStack.Pop();
                return CurrentTarget; // Recurse
            }

            // 2. Registry active
            var active = DebugDrawRegistry.Active;
            if (active != null && GodotObject.IsInstanceValid(active))
                return active;

            // 3. Registry default
            var def = DebugDrawRegistry.Default;
            if (def != null && GodotObject.IsInstanceValid(def))
                return def;

            // 4. No target available
            return null;
        }
    }

    /// <summary>
    /// Create a scoped override for the target component.
    /// </summary>
    /// <param name="component">The component to use for draw calls within the scope.</param>
    /// <returns>A disposable that pops the component when disposed.</returns>
    public static DrawScope Use(DebugDrawComponent component)
    {
        _targetStack.Push(component);
        return new DrawScope();
    }

    internal static void PopTarget()
    {
        if (_targetStack.Count > 0)
            _targetStack.Pop();
    }

    #endregion

    #region Primitives

    /// <summary>
    /// Draw a line from point A to point B.
    /// </summary>
    public static void Line(
        Vector2 a,
        Vector2 b,
        Color color,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f,
        LineCaps lineCaps = default,
        AntiAlias antiAlias = default)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new LineCommand
        {
            From = a,
            To = b,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration,
            LineCaps = lineCaps != default ? lineCaps : target.DefaultLineCaps,
            AntiAlias = antiAlias != default ? antiAlias : target.DefaultAntiAlias
        });
    }

    /// <summary>
    /// Draw a polyline through the given points.
    /// </summary>
    public static void Polyline(
        IReadOnlyList<Vector2> points,
        Color color,
        bool closed = false,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f,
        LineCaps lineCaps = default,
        AntiAlias antiAlias = default)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new PolylineCommand
        {
            Points = new List<Vector2>(points),
            Closed = closed,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration,
            LineCaps = lineCaps != default ? lineCaps : target.DefaultLineCaps,
            AntiAlias = antiAlias != default ? antiAlias : target.DefaultAntiAlias
        });
    }

    /// <summary>
    /// Draw a rectangle.
    /// </summary>
    public static void Rect(
        Rect2 rect,
        Color color,
        bool filled = false,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f,
        LineCaps lineCaps = default,
        AntiAlias antiAlias = default)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new RectCommand
        {
            Rect = rect,
            Filled = filled,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration,
            LineCaps = lineCaps != default ? lineCaps : target.DefaultLineCaps,
            AntiAlias = antiAlias != default ? antiAlias : target.DefaultAntiAlias
        });
    }

    /// <summary>
    /// Draw a circle.
    /// </summary>
    public static void Circle(
        Vector2 center,
        float radius,
        Color color,
        bool filled = false,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f,
        AntiAlias antiAlias = default)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new CircleCommand
        {
            Center = center,
            Radius = radius,
            Filled = filled,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration,
            AntiAlias = antiAlias != default ? antiAlias : target.DefaultAntiAlias
        });
    }

    /// <summary>
    /// Draw an arc.
    /// </summary>
    public static void Arc(
        Vector2 center,
        float radius,
        float startAngle,
        float endAngle,
        Color color,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f,
        AntiAlias antiAlias = default)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new ArcCommand
        {
            Center = center,
            Radius = radius,
            StartAngle = startAngle,
            EndAngle = endAngle,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration,
            AntiAlias = antiAlias != default ? antiAlias : target.DefaultAntiAlias
        });
    }

    /// <summary>
    /// Draw text.
    /// </summary>
    public static void Text(
        Vector2 pos,
        string text,
        Color color,
        DrawFontKind font = DrawFontKind.Mono,
        float size = 14f,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new TextCommand
        {
            Position = pos,
            Text = text,
            FontKind = font,
            FontSize = size,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration
        });
    }

    #endregion

    #region Helper Shapes

    /// <summary>
    /// Draw an arrow from one point to another.
    /// Animation affects shaft length; head appears near end.
    /// </summary>
    public static void Arrow(
        Vector2 from,
        Vector2 to,
        Color color,
        float headSize = 10f,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f,
        LineCaps lineCaps = default,
        AntiAlias antiAlias = default)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new ArrowCommand
        {
            From = from,
            To = to,
            HeadSize = headSize,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration,
            LineCaps = lineCaps != default ? lineCaps : target.DefaultLineCaps,
            AntiAlias = antiAlias != default ? antiAlias : target.DefaultAntiAlias
        });
    }

    /// <summary>
    /// Draw a cross (plus sign) at a center point.
    /// </summary>
    public static void Cross(
        Vector2 center,
        float size,
        Color color,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f,
        LineCaps lineCaps = default,
        AntiAlias antiAlias = default)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new CrossCommand
        {
            Center = center,
            Size = size,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration,
            LineCaps = lineCaps != default ? lineCaps : target.DefaultLineCaps,
            AntiAlias = antiAlias != default ? antiAlias : target.DefaultAntiAlias
        });
    }

    /// <summary>
    /// Draw grid lines within an area.
    /// </summary>
    public static void GridLines(
        Rect2 area,
        Vector2 cellSize,
        Color color,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f,
        LineCaps lineCaps = default,
        AntiAlias antiAlias = default)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new GridLinesCommand
        {
            Area = area,
            CellSize = cellSize,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration,
            LineCaps = lineCaps != default ? lineCaps : target.DefaultLineCaps,
            AntiAlias = antiAlias != default ? antiAlias : target.DefaultAntiAlias
        });
    }

    /// <summary>
    /// Draw grid squares within an area.
    /// </summary>
    public static void GridSquares(
        Rect2 area,
        Vector2 cellSize,
        Color color,
        bool filled = false,
        float thickness = 1f,
        DrawSpace space = DrawSpace.World,
        int layer = 0,
        float duration = 0f,
        float animDuration = 0f,
        LineCaps lineCaps = default,
        AntiAlias antiAlias = default)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(new GridSquaresCommand
        {
            Area = area,
            CellSize = cellSize,
            Filled = filled,
            Color = color,
            Thickness = thickness,
            Space = space,
            Layer = layer,
            TimeRemaining = duration,
            AnimDuration = animDuration,
            LineCaps = lineCaps != default ? lineCaps : target.DefaultLineCaps,
            AntiAlias = antiAlias != default ? antiAlias : target.DefaultAntiAlias
        });
    }

    #endregion

    #region Custom Commands

    /// <summary>
    /// Enqueue a custom DrawCommand.
    /// This allows users to create their own DrawCommand subclasses for custom drawing logic.
    /// </summary>
    /// <param name="command">The custom command to enqueue.</param>
    public static void Enqueue(DrawCommand command)
    {
        var target = CurrentTarget;
        if (target == null) return;

        target.Enqueue(command);
    }

    #endregion

    #region Utility

    /// <summary>
    /// Clear all frame commands on the current target.
    /// </summary>
    public static void ClearFrame()
    {
        CurrentTarget?.ClearFrameCommands();
    }

    /// <summary>
    /// Clear all timed commands on the current target.
    /// </summary>
    public static void ClearTimed()
    {
        CurrentTarget?.ClearTimedCommands();
    }

    /// <summary>
    /// Clear all commands on the current target.
    /// </summary>
    public static void ClearAll()
    {
        CurrentTarget?.ClearAll();
    }

    #endregion
}