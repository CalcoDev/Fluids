using Godot;
using System;

namespace Fluids.Scripts.Wave;

public enum ResetMode
{
    Zero,
    BellCurve
}

public partial class WaveEquation1D : Node2D
{
    [Export] private int PointCount { get; set; } = 500;
    [Export] private float WaveSpeed { get; set; } = 2.0f;
    [Export] private bool WrapAround { get; set; } = false;
    [Export] private float LineLength { get; set; } = 500.0f;
    [Export] private Color LineColor { get; set; } = Colors.White;
    [Export] private float ImpulseStrength { get; set; } = 1.0f;
    [Export] private ResetMode SpaceResetMode { get; set; } = ResetMode.BellCurve;
    [Export] private float MaxCFL { get; set; } = 1.0f;
    [Export] private int SimulationStepCount { get; set; } = 1;

    private float[] _offsetsCurr;
    private float[] _offsets;
    private float[] _offsetsOld;
    private float _brushRadius = 20.0f;
    private Vector2 _lastMousePos = Vector2.Zero;
    private float _lastDeltaTime = 0.0f;
    private bool _wasVisible = true;

    public override void _EnterTree()
    {
        base._EnterTree();
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Space)
        {
            ResetSimulation();
            GetTree().Root.SetInputAsHandled();
            QueueRedraw();
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Middle)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    _brushRadius += 2.0f;
                }
                else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    _brushRadius = Mathf.Max(1.0f, _brushRadius - 2.0f);
                }
                QueueRedraw();
            }
        }

        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector2 mousePos = mouseMotion.Position;

            if (Input.IsMouseButtonPressed(MouseButton.Right))
            {
                HandleRightClickDrag(mousePos);
                GetTree().Root.SetInputAsHandled();
            }
            else if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                HandleLeftClickDrag(mousePos);
                GetTree().Root.SetInputAsHandled();
            }

            _lastMousePos = mousePos;
            QueueRedraw();
        }
    }

    private void HandleRightClickDrag(Vector2 mousePos)
    {
        Vector2 center = GlobalPosition;
        Vector2 relativePos = mousePos - center;

        float incr = LineLength / PointCount;
        float startX = -LineLength / 2.0f;

        for (int i = 0; i < PointCount; ++i)
        {
            float pointX = startX + i * incr;
            Vector2 pointPos = new Vector2(pointX, _offsets[i]);

            float dist = Mathf.Abs(relativePos.X - pointX);
            float yDist = Mathf.Abs(relativePos.Y - _offsets[i]);
            float totalDist = Mathf.Sqrt(dist * dist + yDist * yDist);

            if (totalDist < _brushRadius)
            {
                float influence = 1.0f - (totalDist / _brushRadius);
                _offsets[i] = relativePos.Y * influence + _offsets[i] * (1.0f - influence);
            }
        }
    }

    private void HandleLeftClickDrag(Vector2 mousePos)
    {
        Vector2 center = GlobalPosition;
        Vector2 relativePos = mousePos - center;
        Vector2 mouseVelocity = relativePos - (_lastMousePos - center);

        float incr = LineLength / PointCount;
        float startX = -LineLength / 2.0f;

        for (int i = 0; i < PointCount; ++i)
        {
            float pointX = startX + i * incr;
            float dist = Mathf.Abs(relativePos.X - pointX);
            float yDist = Mathf.Abs(relativePos.Y - _offsets[i]);
            float totalDist = Mathf.Sqrt(dist * dist + yDist * yDist);

            if (totalDist < _brushRadius)
            {
                float influence = 1.0f - (totalDist / _brushRadius);
                _offsets[i] += mouseVelocity.Y * influence * ImpulseStrength * _lastDeltaTime;
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
        SimInit();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Visible)
        {
            _wasVisible = false;
            return;
        }

        if (!_wasVisible)
        {
            ResetSimulation();
            _wasVisible = true;
        }

        _lastDeltaTime = (float)delta;
        for (int step = 0; step < SimulationStepCount; ++step)
        {
            SimStep((float)delta / SimulationStepCount);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();

        SimDraw();
        DrawBrushPreview();
    }

    private void DrawBrushPreview()
    {
        Vector2 center = GlobalPosition;
        Vector2 mousePos = GetLocalMousePosition();
        DrawCircle(mousePos, _brushRadius, new Color(Colors.White, 0.2f));
    }

    private void ResetSimulation()
    {
        if (SpaceResetMode == ResetMode.Zero)
        {
            Array.Clear(_offsets, 0, _offsets.Length);
            Array.Copy(_offsets, _offsetsOld, _offsets.Length);
        }
        else if (SpaceResetMode == ResetMode.BellCurve)
        {
            float centerIdx = (PointCount - 1) / 2.0f;
            float sigma = PointCount / 4.0f;
            float amplitude = GetViewportRect().Size.Y / 2.0f;

            for (int i = 0; i < PointCount; ++i)
            {
                float distFromCenter = MathF.Pow(i - centerIdx, 3.0f);
                _offsets[i] = -amplitude * Mathf.Exp(-MathF.Abs(distFromCenter) / (2.0f * sigma * sigma));
                _offsetsOld[i] = _offsets[i];
            }
        }
    }

    // SIM STUFF
    public void SimInit()
    {
        _offsetsCurr = new float[PointCount];
        _offsets = new float[PointCount];
        _offsetsOld = new float[PointCount];
    }

    public void SimStep(float deltaTime)
    {
        float dx = LineLength / PointCount;
        float lambda = WaveSpeed * deltaTime / dx;
        float lambda2 = lambda * lambda;

        if (lambda > MaxCFL)
        {
            GD.Print($"Clamped CFL: lambda={lambda} to MaxCFL={MaxCFL}");
        }

        lambda = Mathf.Min(lambda, MaxCFL);
        lambda2 = lambda * lambda;

        for (int i = 0; i < PointCount; ++i)
        {
            float prev = (!WrapAround && i == 0) ? 0.0f : _offsets[(i - 1 + PointCount) % PointCount];
            float next = (!WrapAround && i == PointCount - 1) ? 0.0f : _offsets[(i + 1) % PointCount];

            float inertia = 2.0f * _offsets[i] - _offsetsOld[i];
            float lap = prev - 2.0f * _offsets[i] + next;
            _offsetsCurr[i] = inertia + lambda2 * lap;
        }

        if (!WrapAround)
        {
            _offsetsCurr[0] = 0.0f;
            _offsetsCurr[PointCount - 1] = 0.0f;
        }

        Array.Copy(_offsets, _offsetsOld, _offsets.Length);
        Array.Copy(_offsetsCurr, _offsets, _offsetsCurr.Length);
    }

    public void SimDraw()
    {
        float incr = LineLength / PointCount;
        float startX = -LineLength * 0.5f;
        Vector2 center = Vector2.Zero;

        for (int i = 0; i < PointCount - 1; ++i)
        {
            float x1 = startX + i * incr;
            float x2 = startX + (i + 1) * incr;

            float y1 = _offsets[i];
            float y2 = _offsets[i + 1];

            Vector2 p1 = center + new Vector2(x1, y1);
            Vector2 p2 = center + new Vector2(x2, y2);

            DrawLine(p1, p2, LineColor, 1.0f);
        }

        if (WrapAround && PointCount > 1)
        {
            float lastX = startX + (PointCount - 1) * incr;
            float firstX = startX;

            float lastY = _offsets[PointCount - 1];
            float firstY = _offsets[0];

            Vector2 p1 = center + new Vector2(lastX - LineLength, lastY);
            Vector2 p2 = center + new Vector2(firstX, firstY);
            DrawLine(p1, p2, LineColor, 1.0f);

            Vector2 p3 = center + new Vector2(lastX, lastY);
            Vector2 p4 = center + new Vector2(firstX + LineLength, firstY);
            DrawLine(p3, p4, LineColor, 1.0f);
        }
    }
}
