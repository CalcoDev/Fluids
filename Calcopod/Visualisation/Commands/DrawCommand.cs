using Godot;

namespace Calcopod.Visualisation.Commands;

/// <summary>
/// Base class for all debug draw commands.
/// </summary>
public abstract class DrawCommand
{
    public DrawSpace Space { get; set; }
    public int Layer { get; set; }
    public Color Color { get; set; }
    public float Thickness { get; set; } = 1f;
    public LineCaps LineCaps { get; set; } = LineCaps.Flat;
    public AntiAlias AntiAlias { get; set; } = AntiAlias.Disabled;

    // Lifetime
    public float TimeRemaining { get; set; }
    public bool IsTimed => TimeRemaining > 0;

    // Animation
    public float AnimDuration { get; set; }

    /// <summary>
    /// Computes animation progress at draw time.
    /// Returns 1 if no animation, otherwise lerps from 0 to 1 over AnimDuration.
    /// </summary>
    public float GetAnimT()
    {
        if (AnimDuration <= 0) return 1f;
        return Mathf.Clamp(1f - TimeRemaining / AnimDuration, 0f, 1f);
    }

    /// <summary>
    /// Draw this command using the provided CanvasItem.
    /// </summary>
    public abstract void Draw(CanvasItem canvas, DebugDrawComponent component);
}
