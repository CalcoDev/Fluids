using Godot;
using System;

namespace Fluids.scripts.Presentation.Act1;

// [Tool]
public partial class StringDemo : Node2D
{
    private bool _init = false;

    [ExportGroup("References")]
    [Export] public Marker2D StringLeft { get; set; }
    [Export] public Marker2D StringRight { get; set; }

    private int _stringSegmentCount;
    [Export] public int StringSegmentCount
    {
        get => _stringSegmentCount;
        set
        {
            _stringSegmentCount = value;
            if (_init)
                Start();
        }
    }

    [ExportGroup("Color Settings")]
    [Export] private float StringSegmentLineWidth;
    [Export] private Color StringColorA;
    [Export] private Color StringColorB;


    [ExportGroup("Simulation")]
    [Export] private bool DoSimulateGravity;
    [Export] private float GravityAcceleration = 500f;
    [Export] private int ConstraintIterations = 10;

    private float[] _heights;
    private float[] _velocities;
    private float _segmentRestLength;

    private void Start()
    {
        _heights = new float[StringSegmentCount];
        _velocities = new float[StringSegmentCount];

        // Calculate rest length based on segment width
        float totalWidth = StringRight.Position.X - StringLeft.Position.X;
        _segmentRestLength = totalWidth / StringSegmentCount;

        for (int i = 0; i < StringSegmentCount; i++)
        {
            _heights[i] = 0.0f;
            _velocities[i] = 0.0f;
        }
    }

    private void SimulateGravity()
    {
        if (!DoSimulateGravity)
            return;

        float deltaTime = (float)GetPhysicsProcessDeltaTime();

        for (int i = 1; i < StringSegmentCount - 1; i++)
            _velocities[i] += GravityAcceleration * deltaTime;
        // ConstrainDistances();
        for (int i = 1; i < StringSegmentCount - 1; i++)
            _heights[i] += _velocities[i] * deltaTime;
        ConstrainDistances();
    }

    private void ConstrainDistances()
    {
        Vector2 startPos = StringLeft.Position;
        Vector2 endPos = StringRight.Position;
        float segmentWidth = (endPos.X - startPos.X) / StringSegmentCount;

        for (int iter = 0; iter < ConstraintIterations; iter++)
        {
            for (int i = 0; i < StringSegmentCount - 1; i++)
            {
                Vector2 p1 = new Vector2(startPos.X + i * segmentWidth, startPos.Y + _heights[i]);
                Vector2 p2 = new Vector2(startPos.X + (i + 1) * segmentWidth, startPos.Y + _heights[i + 1]);

                Vector2 delta = p2 - p1;
                float currentDistance = delta.Length();

                if (currentDistance < 0.001f)
                    continue;

                float difference = currentDistance - _segmentRestLength;
                float correctionPercent = difference / currentDistance;

                Vector2 correction = delta * correctionPercent;

                bool canMoveP1 = i > 0;
                bool canMoveP2 = i < StringSegmentCount - 2;

                if (canMoveP1 && canMoveP2)
                {
                    float halfCorrectionY = correction.Y * 0.5f;
                    _heights[i] += halfCorrectionY;
                    _heights[i + 1] -= halfCorrectionY;

                    // Dampen velocities based on correction
                    _velocities[i] -= _velocities[i] * Mathf.Abs(halfCorrectionY) * 0.5f;
                    _velocities[i + 1] -= _velocities[i + 1] * Mathf.Abs(halfCorrectionY) * 0.5f;
                }
                else if (canMoveP2)
                {
                    _heights[i + 1] -= correction.Y;
                    _velocities[i + 1] -= _velocities[i + 1] * Mathf.Abs(correction.Y) * 0.5f;
                }
                else if (canMoveP1)
                {
                    _heights[i] += correction.Y;
                    _velocities[i] -= _velocities[i] * Mathf.Abs(correction.Y) * 0.5f;
                }
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        _init = true;
        Start();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        SimulateGravity();
    }

    public override void _Draw()
    {
        base._Draw();

        if (_heights == null || _heights.Length != StringSegmentCount)
            return;

        Vector2 startPos = StringLeft.Position;
        Vector2 endPos = StringRight.Position;
        float segmentWidth = (endPos.X - startPos.X) / StringSegmentCount;

        for (int i = 0; i < StringSegmentCount - 1; i++)
        {
            Color c = (i % 2 == 0) ? StringColorA : StringColorB;
            Vector2 from = new Vector2(startPos.X + i * segmentWidth, startPos.Y + _heights[i]);
            Vector2 to = new Vector2(startPos.X + (i + 1) * segmentWidth, startPos.Y + _heights[i + 1]);
            DrawLine(from, to, c, StringSegmentLineWidth);
        }
    }
}
