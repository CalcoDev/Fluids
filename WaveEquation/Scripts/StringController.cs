using Calcopod.Visualisation;
using Godot;
using System;

namespace Fluids.WaveEquation.Scripts;

public partial class StringController : Node
{
    [ExportGroup("References")]
    [Export] public Marker2D LeftEnd;
    [Export] public Marker2D RightEnd;

    [ExportGroup("String Settings")]
    [Export] public int PointCount;

    public override void _Process(double delta)
    {
    }
}