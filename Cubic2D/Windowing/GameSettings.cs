using System.Drawing;
using Cubic2D.Render;
using Cubic2D.Audio;

namespace Cubic2D.Windowing;

/// <summary>
/// Initial settings for the <see cref="CubicGame"/> on startup.
/// </summary>
public struct GameSettings
{
    /// <summary>
    /// The initial size in pixels (resolution) of the window. (Default: 1280x720)
    /// </summary>
    public Size Size;

    /// <summary>
    /// The initial title of the window. (Default: "Cubic2D Window")
    /// </summary>
    public string Title;

    /// <summary>
    /// The initial <see cref="WindowMode"/> of the window. (Default: Windowed)
    /// </summary>
    public WindowMode WindowMode;
    
    /// <summary>
    /// If true, the window will be resizable. (Default: false)
    /// </summary>
    public bool Resizable;

    /// <summary>
    /// If true, the graphics device will attempt to sync to vertical refresh. (Default: true)
    /// </summary>
    public bool VSync;

    /// <summary>
    /// The FPS (frames per second) the game will attempt to run at. If the value is 0, the game will <b>not</b> limit
    /// the FPS. (Default: 60)
    /// </summary>
    /// <remarks>If <see cref="VSync"/> is enabled, the FPS of the application cannot exceed the refresh rate of the
    /// monitor.</remarks>
    public uint TargetFps;

    /// <summary>
    /// The starting location of the window. If you <b>do not</b> set this value, the window will start centered on
    /// the primary monitor.
    /// </summary>
    public Point Location;

    /// <summary>
    /// If false, the window will remain closed until GameWindow.Visible is set to true. (Default: true)
    /// </summary>
    public bool StartVisible;

    /// <summary>
    /// Set the graphics API Cubic should use. If you don't know which, leave this value, and Cubic will work out which
    /// one to use for you. (Default: Default)
    /// </summary>
    public GraphicsApi Api;

    /// <summary>
    /// The number of channels the <see cref="AudioDevice"/> will have. (Default: 32)
    /// </summary>
    public int AudioChannels;

    public GameSettings()
    {
        Size = new Size(1280, 720);
        Title = "Cubic2D Window";
        WindowMode = WindowMode.Windowed;
        Resizable = false;
        VSync = true;
        TargetFps = 60;
        Location = new Point(-1, -1);
        StartVisible = true;
        Api = GraphicsApi.Default;
        AudioChannels = 32;
    }
}