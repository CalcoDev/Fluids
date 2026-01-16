using Godot;
using System;

namespace Fluids.Scripts.Water.SaintVenand;

public partial class SaintVenandSimulation : Node
{
    #region Exports

    [ExportGroup("References")]
    [Export]
    public CsgBox3D Trench { get; set; }

    [Export]
    public Vector3 Direction { get; set; } = Vector3.Right;


    [ExportGroup("Simulation Settings")]
    [Export(PropertyHint.Range, "8,1024,1")]
    public int NumCells { get; set; } = 256;

    [Export(PropertyHint.Range, "1,32,1")]
    public int SubstepsMax { get; set; } = 8;

    [Export]
    public float G { get; set; } = 9.81f;

    [Export]
    public float ManningN { get; set; } = 0.03f;

    [Export]
    public float BedSlope { get; set; } = 0.0f;

    [Export]
    public float MinDepth { get; set; } = 1e-4f;

    [Export(PropertyHint.Range, "0.1,0.9,0.01")]
    public float Cfl { get; set; } = 0.5f;

    [ExportSubgroup("Inflow")]
    [Export]
    public bool InflowEnabled
    {
        get => _inflowEnabled;
        set
        {
            _inflowEnabled = value;

            if (_simulation != null)
                _simulation.InflowEnabled = value;
        }
    }
    private bool _inflowEnabled = false;

    [Export]
    public float InflowQPerWidth
    {
        get => _inflowQPerWidth;
        set
        {
            _inflowQPerWidth = value;

            if (_simulation != null)
                _simulation.InflowQPerWidth = value;
        }
    }
    private float _inflowQPerWidth;

    [ExportSubgroup("Outflow")]
    [Export]
    public Simulation.OutflowMode OutflowModeValue
    {
        get => _outflowModeValue;
        set
        {
            _outflowModeValue = value;

            if (_simulation != null)
                _simulation.OutflowModeValue = value;
        }
    }
    private Simulation.OutflowMode _outflowModeValue = Simulation.OutflowMode.OpenCopy;

    [Export]
    public float OutflowFixedDepth
    {
        get => _outflowFixedDepth;
        set
        {
            _outflowFixedDepth = value;

            if (_simulation != null)
                _simulation.OutflowFixedDepth = value;
        }
    }
    private float _outflowFixedDepth = 0.2f;

    [ExportGroup("Initial Conditions")]
    [Export]
    public float InitialDepth { get; set; } = 0.5f;

    [Export]
    public float InitialVelocity { get; set; } = 0.0f;

    [ExportGroup("Debug")]
    [Export]
    public bool DebugDraw { get; set; } = true;

    [Export]
    public float DebugScale { get; set; } = 1.0f;

    [Export]
    public bool DebugShowStats { get; set; } = true;

    #endregion


    #region Internal State - Geometry

    private float _channelLength;
    private float _channelWidth;
    private float _bedY;
    private float _dx;
    private Vector3 _axisNormalized;
    private Vector3 _widthDir;
    private Vector3 _upDir;
    private Vector3 _originWorld;

    #endregion

    private bool _initialised;
    private Simulation _simulation;

    #region Lifecycle

    public override void _Ready()
    {
        base._Ready();

        Initialise();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _simulation.Step((float)delta, _channelWidth);

        if (DebugDraw)
        {
            DrawDebug();
        }

        if (DebugShowStats)
        {
            DisplayStats();
        }
    }

    #endregion


    #region Simulation Helpers

    public Vector3 GetCellWorldPosition(int cellIndex)
    {
        float x = (cellIndex + 0.5f) * _dx;
        Vector3 pos = _originWorld + _axisNormalized * x;
        pos.Y = _bedY - BedSlope * x + _simulation.H[cellIndex];
        return pos;
    }

    public void SetCellState(int startIndex, int count, float depth, float velocity)
    {
        int end = Math.Min(startIndex + count, _simulation.Params.NumCells);
        for (int i = startIndex; i < end; i++)
        {
            _simulation.SetDepth(i, depth);
            _simulation.SetDischarge(i, depth * velocity);
        }
    }

    #endregion



    private void Initialise()
    {
        if (Trench == null)
        {
            GD.PrintErr("Trench CSG is not assigned!");
            return;
        }
        // Trench.Visible = false;

        ExtractChannelGeometry();

        _simulation = new Simulation(new Simulation.SimParams(
            NumCells, SubstepsMax, _dx, G, ManningN, BedSlope, MinDepth, Cfl
        ));
        _simulation.InflowEnabled = InflowEnabled;
        _simulation.InflowQPerWidth = InflowQPerWidth;
        _simulation.OutflowModeValue = OutflowModeValue;
        _simulation.OutflowFixedDepth = OutflowFixedDepth;
        _simulation.InitialiseState(InitialDepth, InitialVelocity);

        _initialised = true;
    }

    private void ExtractChannelGeometry()
    {
        // TODO(calco): Currently hard coded for box CSG.
        // Should definetely be upgraded to procedurally obtaining stuff.

        // Get trench transform and size
        Transform3D trenchTransform = Trench.GlobalTransform;
        Vector3 trenchSize = Trench.Size;

        // Normalize direction
        _axisNormalized = Direction.Normalized();
        if (_axisNormalized.LengthSquared() < 0.001f)
        {
            _axisNormalized = Vector3.Right;
            GD.PrintErr("SaintVenantChannel3D: Direction was near-zero, defaulting to X axis");
        }

        // Get the local basis vectors
        Vector3 basisX = trenchTransform.Basis.X;
        Vector3 basisY = trenchTransform.Basis.Y;
        Vector3 basisZ = trenchTransform.Basis.Z;

        // Find which local axis best aligns with direction
        float dotX = Math.Abs(basisX.Dot(_axisNormalized));
        float dotY = Math.Abs(basisY.Dot(_axisNormalized));
        float dotZ = Math.Abs(basisZ.Dot(_axisNormalized));

        Vector3 channelLocalAxis;
        Vector3 widthLocalAxis;
        Vector3 depthLocalAxis;
        float channelLocalSize;
        float widthLocalSize;
        float depthLocalSize;

        if (dotX >= dotY && dotX >= dotZ)
        {
            // X is channel axis
            channelLocalAxis = basisX;
            channelLocalSize = trenchSize.X;
            if (dotY >= dotZ)
            {
                widthLocalAxis = basisZ;
                widthLocalSize = trenchSize.Z;
                depthLocalAxis = basisY;
                depthLocalSize = trenchSize.Y;
            }
            else
            {
                widthLocalAxis = basisY;
                widthLocalSize = trenchSize.Y;
                depthLocalAxis = basisZ;
                depthLocalSize = trenchSize.Z;
            }
        }
        else if (dotY >= dotX && dotY >= dotZ)
        {
            // Y is channel axis
            channelLocalAxis = basisY;
            channelLocalSize = trenchSize.Y;
            widthLocalAxis = basisX;
            widthLocalSize = trenchSize.X;
            depthLocalAxis = basisZ;
            depthLocalSize = trenchSize.Z;
        }
        else
        {
            // Z is channel axis
            channelLocalAxis = basisZ;
            channelLocalSize = trenchSize.Z;
            widthLocalAxis = basisX;
            widthLocalSize = trenchSize.X;
            depthLocalAxis = basisY;
            depthLocalSize = trenchSize.Y;
        }

        // Make sure axis aligns with direction sign
        if (channelLocalAxis.Dot(_axisNormalized) < 0)
        {
            channelLocalAxis = -channelLocalAxis;
        }

        _axisNormalized = channelLocalAxis.Normalized();
        _channelLength = channelLocalSize;
        _channelWidth = widthLocalSize;

        // Determine up direction
        _upDir = Vector3.Up;
        if (Math.Abs(depthLocalAxis.Dot(Vector3.Up)) > 0.9f)
        {
            _upDir = depthLocalAxis.Dot(Vector3.Up) > 0 ? Vector3.Up : Vector3.Down;
        }

        // Width direction (perpendicular to channel axis in horizontal plane)
        _widthDir = widthLocalAxis.Normalized();

        // Compute bed Y (bottom of the trench)
        // Bed is at the bottom of the box
        Vector3 trenchCenter = trenchTransform.Origin;
        _bedY = trenchCenter.Y - depthLocalSize * 0.5f;

        // Origin is at upstream end of channel
        _originWorld = trenchCenter - _axisNormalized * (_channelLength * 0.5f);
        _originWorld.Y = _bedY;

        // Cell spacing
        _dx = _channelLength / NumCells;
    }


    #region Debug

    private void DrawDebug()
    {
        // Draw water surface profile as a line path using DebugDraw3D
        var points = new Vector3[NumCells + 1];

        for (int k = 0; k <= NumCells; k++)
        {
            float x = k * _dx;
            Vector3 centerPos = _originWorld + _axisNormalized * x;

            float depth;
            if (k == 0)
            {
                depth = _simulation.H[0];
            }
            else if (k == NumCells)
            {
                depth = _simulation.H[NumCells - 1];
            }
            else
            {
                depth = 0.5f * (_simulation.H[k - 1] + _simulation.H[k]);
            }

            float bedZ = _bedY - BedSlope * x;
            float surfaceY = bedZ + depth * DebugScale;

            points[k] = centerPos;
            points[k].Y = surfaceY;
        }

        // Draw using DebugDraw3D if available
        DebugDraw3D.DrawLinePath(points, Colors.Cyan);

        // Draw bed line
        var bedPoints = new Vector3[2];
        bedPoints[0] = _originWorld;
        bedPoints[1] = _originWorld + _axisNormalized * _channelLength;
        bedPoints[1].Y = _bedY - BedSlope * _channelLength;
        DebugDraw3D.DrawLine(bedPoints[0], bedPoints[1], Colors.Brown);

        // Draw channel bounds
        Vector3 corner1 = _originWorld + _widthDir * (-_channelWidth * 0.5f);
        Vector3 corner2 = _originWorld + _widthDir * (_channelWidth * 0.5f);
        Vector3 corner3 = bedPoints[1] + _widthDir * (_channelWidth * 0.5f);
        Vector3 corner4 = bedPoints[1] + _widthDir * (-_channelWidth * 0.5f);

        DebugDraw3D.DrawLine(corner1, corner2, Colors.Cyan);
        DebugDraw3D.DrawLine(corner3, corner4, Colors.Cyan);
        DebugDraw3D.DrawLine(corner1, corner4, Colors.Cyan);
        DebugDraw3D.DrawLine(corner2, corner3, Colors.Cyan);
    }

    private void DisplayStats()
    {
        DebugDraw2D.BeginTextGroup("Saint-Venant Water", 0, Colors.Cyan);
        DebugDraw2D.SetText("Cells", NumCells);
        DebugDraw2D.SetText("Max Speed", $"{_simulation.MaxWaveSpeed:F3} m/s");
        DebugDraw2D.SetText("Max Depth", $"{_simulation.MaxDepth:F3} m");
        DebugDraw2D.SetText("Substeps", _simulation.LastSubsteps);
        DebugDraw2D.SetText("dx", $"{_dx:F4} m");
        DebugDraw2D.SetText("Channel L", $"{_channelLength:F2} m");
        DebugDraw2D.SetText("Channel B", $"{_channelWidth:F2} m");
        DebugDraw2D.EndTextGroup();
    }

    #endregion

}
