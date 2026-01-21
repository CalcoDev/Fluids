using Godot;
using System;

namespace Fluids.Scripts.Wave;

public partial class WaveEquation2D : Node2D
{
    [Export] private int Width { get; set; } = 128;
    [Export] private int Height { get; set; } = 128;
    [Export] private float WaveSpeed { get; set; } = 1.0f;
    [Export] private float MaxCFL { get; set; } = 0.7f;
    [Export] private bool WrapAround { get; set; } = false;
    [Export] private float ImpulseStrength { get; set; } = 1.0f;
    [Export] private float BrushRadius { get; set; } = 20.0f;
    [Export] private ResetMode SpaceResetMode { get; set; } = ResetMode.BellCurve;
    [Export] private int SimulationStepCount { get; set; } = 1;
    [Export] private Color ClearColor { get; set; } = Colors.Black;
    [Export] private Color PositiveColor { get; set; } = Colors.Red;
    [Export] private Color NegativeColor { get; set; } = Colors.Cyan;
    [Export] private Color BrushPreviewColor { get; set; } = Colors.White;
    [Export] private float BrushPreviewThickness { get; set; } = 1.0f;

    private float[,] _offsetsCurr;
    private float[,] _offsets;
    private float[,] _offsetsOld;
    private Vector2 _lastMousePos = Vector2.Zero;
    private float _lastDeltaTime = 0.0f;
    private TextureRect _textureRect;
    private Image _image;
    private ImageTexture _imageTexture;
    private bool _wasVisible = true;
    private bool _leftMousePressed = false;
    private bool _rightMousePressed = false;

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
            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                BrushRadius += 2.0f;
                QueueRedraw();
            }
            else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                BrushRadius = Mathf.Max(1.0f, BrushRadius - 2.0f);
                QueueRedraw();
            }
            else if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                _leftMousePressed = mouseButton.Pressed;
                GetTree().Root.SetInputAsHandled();
            }
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                _rightMousePressed = mouseButton.Pressed;
                GetTree().Root.SetInputAsHandled();
            }
        }

        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector2 mousePos = mouseMotion.Position;

            if (_leftMousePressed)
            {
                HandleLeftClick(mousePos);
                GetTree().Root.SetInputAsHandled();
            }
            else if (_rightMousePressed)
            {
                HandleRightClick(mousePos);
                GetTree().Root.SetInputAsHandled();
            }

            _lastMousePos = mousePos;
            QueueRedraw();
        }
    }

    private void HandleRightClick(Vector2 mousePos)
    {
        Vector2 gridSize = GetGridDimensions();
        Vector2 localPos = mousePos - GlobalPosition;
        Vector2 normPos = localPos / gridSize;

        int centerX = (int)(normPos.X * Width);
        int centerY = (int)(normPos.Y * Height);

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist < BrushRadius)
                {
                    float influence = 1.0f - (dist / BrushRadius);
                    _offsets[x, y] -= ImpulseStrength * influence;
                }
            }
        }
    }

    private void HandleLeftClick(Vector2 mousePos)
    {
        Vector2 gridSize = GetGridDimensions();
        Vector2 localPos = mousePos - GlobalPosition;
        Vector2 normPos = localPos / gridSize;

        int centerX = (int)(normPos.X * Width);
        int centerY = (int)(normPos.Y * Height);

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist < BrushRadius)
                {
                    float influence = 1.0f - (dist / BrushRadius);
                    _offsets[x, y] += ImpulseStrength * influence;
                }
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
        SimInit();
        SetupTextureRect();
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

        UpdateTextureDisplay();
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();
        DrawBrushPreview();
    }

    private void DrawBrushPreview()
    {
        Vector2 mousePos = GetLocalMousePosition();
        DrawCircle(mousePos, BrushRadius, new Color(BrushPreviewColor, 0.2f));
        DrawCircle(mousePos, BrushRadius, BrushPreviewColor, false, BrushPreviewThickness);
    }

    private Vector2 GetGridDimensions()
    {
        if (_textureRect == null)
            return Vector2.One;
        return _textureRect.Size;
    }

    private void SetupTextureRect()
    {
        _textureRect = GetChild(0) as TextureRect;
        if (_textureRect == null)
        {
            _textureRect = new TextureRect();
            AddChild(_textureRect);
        }

        _image = Image.CreateEmpty(Width, Height, false, Image.Format.Rgb8);
        _imageTexture = ImageTexture.CreateFromImage(_image);
        _textureRect.Texture = _imageTexture;
    }

    private void UpdateTextureDisplay()
    {
        if (_image == null || _offsets == null)
            return;

        float maxVal = 0.0f;
        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                maxVal = Mathf.Max(maxVal, Mathf.Abs(_offsets[x, y]));
            }
        }

        if (maxVal < 0.001f)
            maxVal = 1.0f;

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                float normalized = _offsets[x, y] / maxVal;
                normalized = Mathf.Clamp(normalized, -1.0f, 1.0f);

                Color col;
                if (normalized > 0.001f)
                {
                    col = ClearColor.Lerp(PositiveColor, normalized);
                }
                else if (normalized < -0.001f)
                {
                    col = ClearColor.Lerp(NegativeColor, -normalized);
                }
                else
                {
                    col = ClearColor;
                }

                _image.SetPixel(x, y, col);
            }
        }

        _imageTexture.Update(_image);
    }

    private void ResetSimulation()
    {
        if (SpaceResetMode == ResetMode.Zero)
        {
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    _offsets[x, y] = 0.0f;
                    _offsetsOld[x, y] = 0.0f;
                }
            }
        }
        else if (SpaceResetMode == ResetMode.BellCurve)
        {
            float centerX = (Width - 1) / 2.0f;
            float centerY = (Height - 1) / 2.0f;
            float sigma = Mathf.Min(Width, Height) / 4.0f;
            float amplitude = 50.0f;

            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distSq = dx * dx + dy * dy;
                    _offsets[x, y] = amplitude * Mathf.Exp(-distSq / (2.0f * sigma * sigma));
                    _offsetsOld[x, y] = _offsets[x, y];
                }
            }
        }
    }

    public void SimInit()
    {
        _offsetsCurr = new float[Width, Height];
        _offsets = new float[Width, Height];
        _offsetsOld = new float[Width, Height];
    }

    public void SimStep(float deltaTime)
    {
        float h = 1.0f;
        float lambda = WaveSpeed * deltaTime / h;
        float lambda2 = lambda * lambda;

        if (lambda > MaxCFL)
        {
            GD.Print($"Clamped CFL: lambda={lambda} to MaxCFL={MaxCFL}");
        }

        lambda = Mathf.Min(lambda, MaxCFL);
        lambda2 = lambda * lambda;


        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                float left = GetWrappedValue(x - 1, y);
                float right = GetWrappedValue(x + 1, y);
                float up = GetWrappedValue(x, y - 1);
                float down = GetWrappedValue(x, y + 1);
                float center = _offsets[x, y];

                float lap = left + right + up + down - 4.0f * center;

                float inertia = 2.0f * center - _offsetsOld[x, y];
                _offsetsCurr[x, y] = inertia + lambda2 * lap;
            }
        }

        if (!WrapAround)
        {
            for (int x = 0; x < Width; ++x)
            {
                _offsetsCurr[x, 0] = 0.0f;
                _offsetsCurr[x, Height - 1] = 0.0f;
            }
            for (int y = 0; y < Height; ++y)
            {
                _offsetsCurr[0, y] = 0.0f;
                _offsetsCurr[Width - 1, y] = 0.0f;
            }
        }

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                _offsetsOld[x, y] = _offsets[x, y];
                _offsets[x, y] = _offsetsCurr[x, y];
            }
        }
    }

    private float GetWrappedValue(int x, int y)
    {
        if (WrapAround)
        {
            x = ((x % Width) + Width) % Width;
            y = ((y % Height) + Height) % Height;
            return _offsets[x, y];
        }
        else
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return 0.0f;
            return _offsets[x, y];
        }
    }
}
