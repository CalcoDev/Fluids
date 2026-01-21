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
    private byte[] _rgba;

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

        float radiusSq = BrushRadius * BrushRadius;
        int minX = Mathf.Max(0, (int)(centerX - BrushRadius));
        int maxX = Mathf.Min(Width - 1, (int)(centerX + BrushRadius));
        int minY = Mathf.Max(0, (int)(centerY - BrushRadius));
        int maxY = Mathf.Min(Height - 1, (int)(centerY + BrushRadius));

        for (int y = minY; y <= maxY; ++y)
        {
            for (int x = minX; x <= maxX; ++x)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                float distSq = dx * dx + dy * dy;

                if (distSq < radiusSq)
                {
                    float influence = 1.0f - (Mathf.Sqrt(distSq) / BrushRadius);
                    _offsets[x, y] -= ImpulseStrength * influence;
                    _offsetsOld[x, y] -= ImpulseStrength * influence;
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

        float radiusSq = BrushRadius * BrushRadius;
        int minX = Mathf.Max(0, (int)(centerX - BrushRadius));
        int maxX = Mathf.Min(Width - 1, (int)(centerX + BrushRadius));
        int minY = Mathf.Max(0, (int)(centerY - BrushRadius));
        int maxY = Mathf.Min(Height - 1, (int)(centerY + BrushRadius));

        for (int y = minY; y <= maxY; ++y)
        {
            for (int x = minX; x <= maxX; ++x)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                float distSq = dx * dx + dy * dy;

                if (distSq < radiusSq)
                {
                    float influence = 1.0f - (Mathf.Sqrt(distSq) / BrushRadius);
                    _offsets[x, y] += ImpulseStrength * influence;
                    _offsetsOld[x, y] += ImpulseStrength * influence;
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

        _image = Image.CreateEmpty(Width, Height, false, Image.Format.Rgba8);
        _imageTexture = ImageTexture.CreateFromImage(_image);
        _textureRect.Texture = _imageTexture;
        _rgba = new byte[Width * Height * 4];
    }

    private void UpdateTextureDisplay()
    {
        if (_image == null || _offsets == null || _rgba == null)
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

        float invMax = 1.0f / maxVal;

        System.Threading.Tasks.Parallel.For(0, Height, y =>
        {
            int rowStart = y * Width * 4;
            int idx = rowStart;

            for (int x = 0; x < Width; ++x)
            {
                float normalized = _offsets[x, y] * invMax;
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

                _rgba[idx++] = (byte)(col.R * 255f);
                _rgba[idx++] = (byte)(col.G * 255f);
                _rgba[idx++] = (byte)(col.B * 255f);
                _rgba[idx++] = 255;
            }
        });

        _image.SetData(Width, Height, false, _image.GetFormat(), _rgba);
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

        if (WrapAround)
        {
            SimStepWraparound(lambda2);
        }
        else
        {
            SimStepFixed(lambda2);
        }

        var tmp = _offsetsOld;
        _offsetsOld = _offsets;
        _offsets = _offsetsCurr;
        _offsetsCurr = tmp;
    }

    private void SimStepFixed(float lambda2)
    {
        System.Threading.Tasks.Parallel.For(1, Height - 1, y =>
        {
            for (int x = 1; x < Width - 1; ++x)
            {
                float left = _offsets[x - 1, y];
                float right = _offsets[x + 1, y];
                float up = _offsets[x, y - 1];
                float down = _offsets[x, y + 1];
                float center = _offsets[x, y];

                float lap = left + right + up + down - 4.0f * center;
                float inertia = 2.0f * center - _offsetsOld[x, y];
                _offsetsCurr[x, y] = inertia + lambda2 * lap;
            }
        });

        for (int x = 0; x < Width; ++x)
        {
            _offsetsCurr[x, 0] = 0.0f;
            _offsetsCurr[x, Height - 1] = 0.0f;
        }
        for (int y = 1; y < Height - 1; ++y)
        {
            _offsetsCurr[0, y] = 0.0f;
            _offsetsCurr[Width - 1, y] = 0.0f;
        }
    }

    private void SimStepWraparound(float lambda2)
    {
        System.Threading.Tasks.Parallel.For(0, Height, y =>
        {
            for (int x = 0; x < Width; ++x)
            {
                int xLeft = x == 0 ? Width - 1 : x - 1;
                int xRight = x == Width - 1 ? 0 : x + 1;
                int yUp = y == 0 ? Height - 1 : y - 1;
                int yDown = y == Height - 1 ? 0 : y + 1;

                float left = _offsets[xLeft, y];
                float right = _offsets[xRight, y];
                float up = _offsets[x, yUp];
                float down = _offsets[x, yDown];
                float center = _offsets[x, y];

                float lap = left + right + up + down - 4.0f * center;
                float inertia = 2.0f * center - _offsetsOld[x, y];
                _offsetsCurr[x, y] = inertia + lambda2 * lap;
            }
        });
    }
}
