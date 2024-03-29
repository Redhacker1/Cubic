using System.Drawing;
using Cubic.Audio;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.Windowing;

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
    /// The refresh rate that the monitor should use. Note: This is <b>not</b> the same as <see cref="TargetFps"/>, as
    /// this setting directly affects the monitor. (Default: Monitor's default refresh rate)
    /// </summary>
    public uint RefreshRate;

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
    public GraphicsApi GraphicsApi;

    /// <summary>
    /// The number of channels the <see cref="AudioDevice"/> will have. (Default: 32)
    /// </summary>
    public int AudioChannels;

    /// <summary>
    /// The icon the game will use on startup. (Default: None)
    /// </summary>
    public Bitmap Icon;

    /// <summary>
    /// If greater than 0, MSAA anti-aliasing will be enabled. (Default: 0)
    /// </summary>
    public uint MsaaSamples;

    /// <summary>
    /// Set this to false if you don't want to create the default UI font (if you are using your own).
    /// Disabling this will mean the default font won't use any system memory. (Default: true)
    /// </summary>
    public bool CreateDefaultFont;

    public GameSettings()
    {
        Size = new Size(1280, 720);
        Title = "Cubic Window";
        WindowMode = WindowMode.Windowed;
        Resizable = false;
        RefreshRate = 0;
        VSync = true;
        TargetFps = 60;
        Location = new Point(-1, -1);
        StartVisible = true;
        GraphicsApi = GraphicsApi.Default;
        AudioChannels = 32;
        Icon = default;
        MsaaSamples = 0;
        CreateDefaultFont = true;
    }
}