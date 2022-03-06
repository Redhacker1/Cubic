namespace Cubic2D.Render;

public enum TextureSample
{
    /// <summary>
    /// Use linear interpolation for texture scaling.
    /// </summary>
    Linear,
    
    /// <summary>
    /// Use nearest neighbour (point sampling) interpolation for texture scaling. This option is best suited for older
    /// style games.
    /// </summary>
    Nearest
}