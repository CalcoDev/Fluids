#nullable enable
using Calcopod.Visualisation.Commands;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Calcopod.Visualisation;

/// <summary>
/// Main user-facing node for debug drawing.
/// Spawn this anywhere (like a camera rig) to enable debug visualization.
/// </summary>
[GlobalClass]
public partial class DebugDrawComponent : Node
{
    #region Exports

    /// <summary>
    /// If true, this component becomes the global active target for Draw.* calls.
    /// </summary>
    [Export]
    public bool Active
    {
        get => _active;
        set
        {
            if (_active != value)
            {
                _active = value;
                DebugDrawRegistry.MarkDirty();
            }
        }
    }
    private bool _active = true;

    /// <summary>
    /// Priority for active selection. Higher priority wins when multiple components are active.
    /// </summary>
    [Export]
    public int Priority
    {
        get => _priority;
        set
        {
            if (_priority != value)
            {
                _priority = value;
                DebugDrawRegistry.MarkDirty();
            }
        }
    }
    private int _priority = 0;

    /// <summary>
    /// Default layer for draw commands.
    /// </summary>
    [Export]
    public int DefaultLayer { get; set; } = 0;

    /// <summary>
    /// Default thickness for draw commands.
    /// </summary>
    [Export]
    public float DefaultThickness { get; set; } = 1f;

    /// <summary>
    /// Default coordinate space for draw commands.
    /// </summary>
    [Export]
    public DrawSpace DefaultSpace { get; set; } = DrawSpace.World;

    #endregion

    #region Fonts

    /// <summary>
    /// Monospace font for text rendering.
    /// </summary>
    [ExportGroup("Fonts")]
    [Export]
    public Font? MonoFont { get; set; }

    /// <summary>
    /// Serif font for text rendering.
    /// </summary>
    [Export]
    public Font? SerifFont { get; set; }

    /// <summary>
    /// Pixel font for text rendering.
    /// </summary>
    [Export]
    public Font? PixelFont { get; set; }

    /// <summary>
    /// Gets the font for the specified kind.
    /// Falls back to ThemeDB default if not set.
    /// </summary>
    public Font GetFont(DrawFontKind kind)
    {
        Font? font = kind switch
        {
            DrawFontKind.Mono => MonoFont,
            DrawFontKind.Serif => SerifFont,
            DrawFontKind.Pixel => PixelFont,
            _ => null
        };

        return font ?? ThemeDB.FallbackFont;
    }

    #endregion

    #region Surfaces

    private WorldSurface? _worldSurface;
    private ScreenSurface? _screenSurface;
    private CanvasLayer? _screenCanvasLayer;

    public WorldSurface? WorldSurface => _worldSurface;
    public ScreenSurface? ScreenSurface => _screenSurface;

    #endregion

    #region Command Buffers

    private readonly List<DrawCommand> _frameCommandsWorld = new();
    private readonly List<DrawCommand> _timedCommandsWorld = new();
    private readonly List<DrawCommand> _frameCommandsScreen = new();
    private readonly List<DrawCommand> _timedCommandsScreen = new();

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        EnsureSurfacesExist();
        DebugDrawRegistry.Register(this);
    }

    public override void _Ready()
    {
        EnsureSurfacesExist();
    }

    public override void _ExitTree()
    {
        DebugDrawRegistry.Unregister(this);
    }

    public override void _Process(double delta)
    {
        float deltaF = (float)delta;

        // Update timed commands
        UpdateTimedCommands(_timedCommandsWorld, deltaF);
        UpdateTimedCommands(_timedCommandsScreen, deltaF);

        // Queue redraw on surfaces
        _worldSurface?.QueueRedraw();
        _screenSurface?.QueueRedraw();

        // Clear frame commands at end of frame
        _frameCommandsWorld.Clear();
        _frameCommandsScreen.Clear();
    }

    private void UpdateTimedCommands(List<DrawCommand> commands, float delta)
    {
        for (int i = commands.Count - 1; i >= 0; i--)
        {
            commands[i].TimeRemaining -= delta;
            if (commands[i].TimeRemaining <= 0)
            {
                commands.RemoveAt(i);
            }
        }
    }

    private void EnsureSurfacesExist()
    {
        // Create world surface if missing
        if (_worldSurface == null || !IsInstanceValid(_worldSurface))
        {
            _worldSurface = GetNodeOrNull<WorldSurface>("WorldSurface");
            if (_worldSurface == null)
            {
                _worldSurface = new WorldSurface { Name = "WorldSurface" };
                AddChild(_worldSurface);
            }
            _worldSurface.Initialize(this);
        }

        // Create screen canvas layer and surface if missing
        if (_screenCanvasLayer == null || !IsInstanceValid(_screenCanvasLayer))
        {
            _screenCanvasLayer = GetNodeOrNull<CanvasLayer>("ScreenCanvasLayer");
            if (_screenCanvasLayer == null)
            {
                _screenCanvasLayer = new CanvasLayer { Name = "ScreenCanvasLayer" };
                _screenCanvasLayer.Layer = 100; // High layer to be on top
                AddChild(_screenCanvasLayer);
            }
        }

        if (_screenSurface == null || !IsInstanceValid(_screenSurface))
        {
            _screenSurface = _screenCanvasLayer.GetNodeOrNull<ScreenSurface>("ScreenSurface");
            if (_screenSurface == null)
            {
                _screenSurface = new ScreenSurface { Name = "ScreenSurface" };
                _screenCanvasLayer.AddChild(_screenSurface);
            }
            _screenSurface.Initialize(this);
        }
    }

    #endregion

    #region Command Access

    /// <summary>
    /// Gets all world-space commands for rendering (timed + frame).
    /// </summary>
    public IEnumerable<DrawCommand> GetWorldCommands()
    {
        return _timedCommandsWorld.Concat(_frameCommandsWorld);
    }

    /// <summary>
    /// Gets all screen-space commands for rendering (timed + frame).
    /// </summary>
    public IEnumerable<DrawCommand> GetScreenCommands()
    {
        return _timedCommandsScreen.Concat(_frameCommandsScreen);
    }

    #endregion

    #region Command Enqueueing

    /// <summary>
    /// Enqueue a draw command.
    /// </summary>
    public void Enqueue(DrawCommand command)
    {
        bool isTimed = command.TimeRemaining > 0;
        bool isWorld = command.Space == DrawSpace.World;

        if (isWorld)
        {
            if (isTimed)
                _timedCommandsWorld.Add(command);
            else
                _frameCommandsWorld.Add(command);
        }
        else
        {
            if (isTimed)
                _timedCommandsScreen.Add(command);
            else
                _frameCommandsScreen.Add(command);
        }
    }

    #endregion

    #region Clear Methods

    /// <summary>
    /// Clear all frame commands.
    /// </summary>
    public void ClearFrameCommands()
    {
        _frameCommandsWorld.Clear();
        _frameCommandsScreen.Clear();
    }

    /// <summary>
    /// Clear all timed commands.
    /// </summary>
    public void ClearTimedCommands()
    {
        _timedCommandsWorld.Clear();
        _timedCommandsScreen.Clear();
    }

    /// <summary>
    /// Clear all commands.
    /// </summary>
    public void ClearAll()
    {
        ClearFrameCommands();
        ClearTimedCommands();
    }

    #endregion
}
