#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;
using Calcopod.Visualisation.Commands;

namespace Calcopod.Visualisation;

/// <summary>
/// Node2D surface for rendering world-space debug draw commands.
/// </summary>
public partial class WorldSurface : Node2D
{
    private DebugDrawComponent? _owner;

    public void Initialize(DebugDrawComponent owner)
    {
        _owner = owner;
    }

    public override void _Draw()
    {
        if (_owner == null) return;

        var commands = _owner.GetWorldCommands();
        RenderCommands(commands);
    }

    private void RenderCommands(IEnumerable<DrawCommand> commands)
    {
        if (_owner == null) return;

        var grouped = commands
            .GroupBy(c => c.Layer)
            .OrderBy(g => g.Key);

        foreach (var layerGroup in grouped)
        {
            foreach (var command in layerGroup)
            {
                command.Draw(this, _owner);
            }
        }
    }
}
