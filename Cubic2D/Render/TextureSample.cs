namespace Cubic2D.Render;

public enum TextureSample
{
    /// <summary>
    /// Use linear to provide typical texture upscaling.
    /// </summary>
    Linear,
    
    /// <summary>
    /// Use point to provide pixel texture upscaling. This is better suited for older pixel-style games.
    /// </summary>
    Point
}