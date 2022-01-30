using System;
using System.Drawing;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using static Veldrid.Sdl2.Sdl2Native;
using Point = System.Drawing.Point;
using Rectangle = Veldrid.Rectangle;

namespace Cubic2D.Windowing;

public sealed unsafe class GameWindow
{
    internal Sdl2Window SdlWindow;
    private GameSettings _settings;

    private string _title;
    
    #region Public properties

    /// <summary>
    /// If true, the window will automatically center itself on the primary display if <see cref="Size"/> is updated.
    /// Changing the <see cref="Location"/> will automatically disable auto centering. Calling <see cref="CenterWindow"/>
    /// manually won't affect this value.
    /// </summary>
    public bool AutoCenter;
    
    public Size Size
    {
        get => new Size(SdlWindow.Width, SdlWindow.Height);
        set
        {
            SdlWindow.Width = value.Width;
            SdlWindow.Height = value.Height;
            if (AutoCenter)
                CenterWindow();
        }
    }

    public Point Location
    {
        get => new Point(SdlWindow.X, SdlWindow.Y);
        set
        {
            SdlWindow.X = value.X;
            SdlWindow.Y = value.Y;
            AutoCenter = false;
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            SdlWindow.Title = value;
        }
    }

    public bool Resizable
    {
        get => SdlWindow.Resizable;
        set => SdlWindow.Resizable = value;
    }

    public bool Visible
    {
        get => SdlWindow.Visible;
        set => SdlWindow.Visible = value;
    }

    public WindowMode WindowMode
    {
        get
        {
            return SdlWindow.WindowState switch
            {
                WindowState.Normal => WindowMode.Windowed,
                WindowState.FullScreen => WindowMode.Fullscreen,
                WindowState.Maximized => WindowMode.Windowed,
                WindowState.Minimized => WindowMode.Windowed,
                WindowState.BorderlessFullScreen => WindowMode.BorderlessFullscreen,
                WindowState.Hidden => WindowMode.Windowed,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        set
        {
            SdlWindow.WindowState = value switch
            {
                WindowMode.Windowed => WindowState.Normal,
                WindowMode.Fullscreen => WindowState.FullScreen,
                WindowMode.BorderlessFullscreen => WindowState.BorderlessFullScreen,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
    }
    
    #endregion
    
    #region Public methods

    /// <summary>
    /// Center the window on the given display index. (Default: 0, which is the primary display)
    /// </summary>
    public void CenterWindow(int display = 0)
    {
        Rectangle bounds;
        SDL_GetDisplayBounds(display, &bounds);
        SdlWindow.X = bounds.X + bounds.Width / 2 - SdlWindow.Width / 2;
        SdlWindow.Y = bounds.Y + bounds.Height / 2 - SdlWindow.Height / 2;
    }
    
    #endregion
    
    internal GameWindow(GameSettings settings)
    {
        _settings = settings;
    }

    // Prepare window for running.
    internal void Prepare()
    {
        SdlWindow = VeldridStartup.CreateWindow(new WindowCreateInfo(0, 0, _settings.Size.Width, _settings.Size.Height,
            WindowState.Hidden, _settings.Title));
        _title = _settings.Title;
        SdlWindow.Resizable = _settings.Resizable;

        if (_settings.Location != new Point(-1, -1))
            Location = _settings.Location;
        else
        {
            AutoCenter = true;
            CenterWindow();
        }
    }
}