using System;
using System.Drawing;
using Cubic.Utilities;
using Silk.NET.GLFW;
using GMonitor = Silk.NET.GLFW.Monitor;

namespace Cubic.Windowing;

public sealed unsafe class GameWindow : IDisposable
{
    internal static Glfw GLFW;
    
    internal WindowHandle* Handle;
    private GameSettings _settings;

    private string _title;

    private bool _visible;

    private GlfwCallbacks.KeyCallback _keyCallback;
    private GlfwCallbacks.MouseButtonCallback _mouseCallback;
    private GlfwCallbacks.ScrollCallback _scrollCallback;
    private GlfwCallbacks.WindowSizeCallback _sizeCallback;
    private GlfwCallbacks.CharCallback _charCallback;

    public event OnResize Resize;
    
    #region Public properties

    /// <summary>
    /// If true, the window will automatically center itself on the primary display if <see cref="Size"/> is updated.
    /// Changing the <see cref="Location"/> will automatically disable auto centering. Calling <see cref="CenterWindow"/>
    /// manually won't affect this value.
    /// </summary>
    public bool AutoCenter;
    
    public Size Size
    {
        get
        {
            GLFW.GetWindowSize(Handle, out int width, out int height);
            return new Size(width, height);
        }
        set
        {
            GLFW.SetWindowSize(Handle, value.Width, value.Height);
            if (AutoCenter)
                CenterWindow();
        }
    }

    public Point Location
    {
        get
        {
            GLFW.GetWindowPos(Handle, out int x, out int y);
            return new Point(x, y);
        }
        set
        {
            GLFW.SetWindowPos(Handle, value.X, value.Y);
            AutoCenter = false;
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            GLFW.SetWindowTitle(Handle, value);
        }
    }

    /*public bool Resizable
    {
        get => SdlWindow.Resizable;
        set => SdlWindow.Resizable = value;
    }*/

    public bool Visible
    {
        get => _visible;
        set
        {
            if (value)
                GLFW.ShowWindow(Handle);
            else
                GLFW.HideWindow(Handle);
        }
    }

    public WindowMode WindowMode
    {
        get => GLFW.GetWindowMonitor(Handle) != null ? WindowMode.Fullscreen : WindowMode.Windowed;
        set
        {
            switch (value)
            {
                case WindowMode.Windowed:
                    GLFW.SetWindowMonitor(Handle, null, 0, 0, Size.Width, Size.Height, Glfw.DontCare);
                    AutoCenter = true;
                    CenterWindow();
                    break;
                case WindowMode.Fullscreen:
                    GMonitor* monitor = GLFW.GetPrimaryMonitor();
                    VideoMode* mode = GLFW.GetVideoMode(monitor);
                    GLFW.SetWindowMonitor(Handle, monitor, 0, 0, Size.Width, Size.Height,
                        mode->RefreshRate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
    
    #endregion
    
    #region Public methods

    /// <summary>
    /// Center the window on the given display index. (Default: 0, which is the primary display)
    /// </summary>
    public void CenterWindow(int display = 0)
    {
        Monitor disp = Monitors.AttachedMonitors[display];
        DisplayMode mode = disp.CurrentDisplayMode;
        GLFW.SetWindowPos(Handle, disp.Position.X + mode.Resolution.Width / 2 - Size.Width / 2,
            disp.Position.Y + mode.Resolution.Height / 2 - Size.Height / 2);
    }

    public void SetWindowFullscreen(bool fullscreen, Size resolution, int refreshRate = 0)
    {
        Monitor monitor = Monitors.PrimaryMonitor;
        DisplayMode mode = monitor.CurrentDisplayMode;
        int posX = fullscreen ? 0 : monitor.Position.X + mode.Resolution.Width / 2 - resolution.Width / 2;
        int posY = fullscreen ? 0 : monitor.Position.Y + mode.Resolution.Height / 2 - resolution.Height / 2;
        GLFW.SetWindowMonitor(Handle, fullscreen ? GLFW.GetPrimaryMonitor() : null, posX, posY, resolution.Width,
            resolution.Height, fullscreen ? refreshRate > 0 ? refreshRate : mode.RefreshRate : Glfw.DontCare);
        if (!fullscreen)
            AutoCenter = true;
    }
    
    #endregion
    
    internal GameWindow(GameSettings settings)
    {
        _settings = settings;

        _keyCallback = Input.KeyCallback;
        _mouseCallback = Input.MouseCallback;
        _scrollCallback = Input.ScrollCallback;
        _sizeCallback = WindowSizeChanged;
        _charCallback = Input.CharCallback;
    }

    private void WindowSizeChanged(WindowHandle* window, int width, int height)
    {
        Resize?.Invoke(new Size(width, height));
    }

    // Prepare window for running.
    internal void Prepare()
    {
        GLFW = Glfw.GetApi();
        
        if (!GLFW.Init())
            throw new CubicException("GLFW could not initialise.");
        
        GLFW.WindowHint(WindowHintBool.Visible, false);
        GLFW.WindowHint(WindowHintBool.Resizable, _settings.Resizable);
        GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 3);
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 3);
        GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
        GLFW.WindowHint(WindowHintInt.Samples, (int) _settings.MsaaSamples);

        GMonitor* monitor = GLFW.GetPrimaryMonitor();
        VideoMode* mode = GLFW.GetVideoMode(monitor);
        GLFW.WindowHint(WindowHintInt.RedBits, mode->RedBits);
        GLFW.WindowHint(WindowHintInt.GreenBits, mode->GreenBits);
        GLFW.WindowHint(WindowHintInt.BlueBits, mode->BlueBits);
        GLFW.WindowHint(WindowHintInt.RefreshRate, mode->RefreshRate);
        Handle = GLFW.CreateWindow(_settings.Size.Width, _settings.Size.Height, _settings.Title, null, null);
        _title = _settings.Title;

        if (Handle == null)
        {
            GLFW.Terminate();
            throw new CubicException("Window was not created.");
        }

        if (_settings.Icon != default)
        {
            // The icon must be an RGBA image otherwise it won't work - we convert it if it is not in RGBA colour space.
            if (_settings.Icon.ColorSpace != ColorSpace.RGBA)
                _settings.Icon = Bitmap.ConvertToColorSpace(_settings.Icon, ColorSpace.RGBA);
            //fixed (byte* p = _settings.Icon.Data)
           // {
            //    GLFW.SetWindowIcon(Handle,
            //        new ReadOnlySpan<Image>(new Image[]
            //            { new Image(_settings.Icon.Size.Width, _settings.Icon.Size.Height, p) }));
            //}
        }

        GLFW.SetKeyCallback(Handle, _keyCallback);
        GLFW.SetMouseButtonCallback(Handle, _mouseCallback);
        GLFW.SetScrollCallback(Handle, _scrollCallback);
        GLFW.SetWindowSizeCallback(Handle, _sizeCallback);
        GLFW.SetCharCallback(Handle, _charCallback);
        
        GLFW.MakeContextCurrent(Handle);

        if (_settings.Location != new Point(-1, -1))
            Location = _settings.Location;
        else
        {
            AutoCenter = true;
            CenterWindow();
        }
    }

    internal bool ShouldClose
    {
        get => GLFW.WindowShouldClose(Handle);
        set => GLFW.SetWindowShouldClose(Handle, value);
    }

    public delegate void OnResize(Size size);

    public void Dispose()
    {
        GLFW.Terminate();
    }
}