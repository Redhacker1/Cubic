using System;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using static Veldrid.Sdl2.Sdl2Native;

namespace Cubic2D.Windowing;

public sealed unsafe class GameWindow
{
    internal Sdl2Window SdlWindow;
    private GameSettings _settings;

    public void CenterWindow()
    {
        Rectangle bounds;
        SDL_GetDisplayBounds(0, &bounds);
        SdlWindow.X = bounds.X + bounds.Width / 2 - SdlWindow.Width / 2;
        SdlWindow.Y = bounds.Y + bounds.Height / 2 - SdlWindow.Height / 2;
    }
    
    internal GameWindow(GameSettings settings)
    {
        _settings = settings;
    }

    // Prepare window for running.
    internal void Prepare()
    {
        SdlWindow = VeldridStartup.CreateWindow(new WindowCreateInfo(0, 0, _settings.Size.Width, _settings.Size.Height,
            WindowState.Hidden, _settings.Title));
        
        // uuuhhhhh anything else need to go here? i don't think so
    }
}