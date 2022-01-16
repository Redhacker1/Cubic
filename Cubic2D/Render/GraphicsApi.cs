namespace Cubic2D.Render;

public enum GraphicsApi
{
    /// <summary>
    /// Cubic will automatically decide which platform to use based on the current platform (OS) and graphics
    /// specifications.
    /// </summary>
    Default,
    
    /// <summary>
    /// Direct3D 11 backend. Works on Windows.
    /// </summary>
    Direct3D,
    
    /// <summary>
    /// Vulkan backend. Works on Windows, Linux, Android, and MacOS X using MoltenVK.
    /// </summary>
    Vulkan,
    
    /// <summary>
    /// OpenGL backend. Works on all desktop platforms.
    /// </summary>
    OpenGL,
    
    /// <summary>
    /// OpenGLES backend. Works on all platforms, however works best on mobile platforms.
    /// </summary>
    OpenGLES,
    
    /// <summary>
    /// Metal backend. Works on MacOS X and iOS devices.
    /// </summary>
    Metal
}