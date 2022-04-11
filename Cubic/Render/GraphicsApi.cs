namespace Cubic.Render;

public enum GraphicsApi
{
    /// <summary>
    /// Cubic will automatically decide which platform to use based on the current platform (OS) and graphics
    /// specifications.
    /// </summary>
    Default,

    /// <summary>
    /// OpenGL backend. Works on all desktop platforms.
    /// </summary>
    OpenGL,
    
    /// <summary>
    /// OpenGLES backend. Works on all platforms, however works best on mobile platforms.
    /// </summary>
    OpenGLES
}