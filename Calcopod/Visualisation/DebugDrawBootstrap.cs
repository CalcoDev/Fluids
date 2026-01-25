#nullable enable
using Godot;

namespace Calcopod.Visualisation;

/// <summary>
/// Optional autoload node that sets up a default DebugDrawComponent.
/// Add this as an autoload in Project Settings to have debug drawing available globally.
/// </summary>
[GlobalClass]
public partial class DebugDrawBootstrap : Node
{
    private DebugDrawComponent? _defaultComponent;

    [Export]
    public Font? DefaultMonoFont { get; set; }

    [Export]
    public Font? DefaultSerifFont { get; set; }

    [Export]
    public Font? DefaultPixelFont { get; set; }

    public override void _Ready()
    {
        // Create the default debug draw component
        _defaultComponent = new DebugDrawComponent
        {
            Name = "DefaultDebugDraw",
            Active = true,
            Priority = -1000, // Low priority so user components can override
            MonoFont = DefaultMonoFont,
            SerifFont = DefaultSerifFont,
            PixelFont = DefaultPixelFont
        };

        AddChild(_defaultComponent);

        // Register as the default
        DebugDrawRegistry.Default = _defaultComponent;

        GD.Print("[DebugDrawBootstrap] Default DebugDrawComponent created and registered.");
    }

    public override void _ExitTree()
    {
        if (DebugDrawRegistry.Default == _defaultComponent)
        {
            DebugDrawRegistry.Default = null;
        }
    }

    /// <summary>
    /// Gets the default DebugDrawComponent created by this bootstrap.
    /// </summary>
    public DebugDrawComponent? DefaultComponent => _defaultComponent;
}
