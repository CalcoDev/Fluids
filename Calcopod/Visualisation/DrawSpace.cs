namespace Calcopod.Visualisation;

/// <summary>
/// Coordinate space for debug draw commands.
/// </summary>
public enum DrawSpace
{
    /// <summary>
    /// World coordinates - drawn by WorldSurface (Node2D._Draw()).
    /// Natural world transform/camera rules apply.
    /// </summary>
    World,

    /// <summary>
    /// Screen coordinates - drawn by ScreenSurface (Control._Draw()).
    /// Coords are in viewport's UI space.
    /// </summary>
    Screen
}
