namespace Cubic.Render;

public enum GraphicsApi
{
    /// <summary>
    /// Cubic will automatically decide which platform to use based on the current platform (OS) and graphics
    /// specifications.
    /// </summary>
    Default,

    /// <summary>
    /// OpenGL 3.3
    /// </summary>
    OpenGL33,
}