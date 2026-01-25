#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;
using Calcopod.Visualisation.Commands;

namespace Calcopod.Visualisation;

/// <summary>
/// Control surface for rendering screen-space debug draw commands.
/// </summary>
public partial class ScreenSurface : Control
{
    private DebugDrawComponent? _owner;

    public void Initialize(DebugDrawComponent owner)
    {
        _owner = owner;

        // Make it fill the viewport and not block input
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        if (_owner == null) return;

        var commands = _owner.GetScreenCommands();
        RenderCommands(commands);
    }

    private void RenderCommands(IEnumerable<DrawCommand> commands)
    {
        if (_owner == null) return;

        // Group by layer and sort
        var grouped = commands
            .GroupBy(c => c.Layer)
            .OrderBy(g => g.Key);

        foreach (var layerGroup in grouped)
        {
            // Within each layer, maintain insertion order (list order)
            foreach (var command in layerGroup)
            {
                command.Draw(this, _owner);
            }
        }
    }
}
