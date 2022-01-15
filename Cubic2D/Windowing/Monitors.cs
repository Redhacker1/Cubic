using System.Collections.Generic;
using System.Drawing;
using static Veldrid.Sdl2.Sdl2Native;
using Point = Veldrid.Point;
using Rectangle = Veldrid.Rectangle;

namespace Cubic2D.Windowing;

public static unsafe class Monitors
{
    public static int Count => SDL_GetNumVideoDisplays();
    
    public static Monitor GetPrimaryMonitor() => GetMonitor(0);

    public static Monitor GetMonitor(int index)
    {
        Rectangle bounds;
        SDL_GetDisplayBounds(0, &bounds);

        Monitor monitor = new Monitor()
        {
            Position = new Point(bounds.X, bounds.Y),
            Resolution = new Size(bounds.Width, bounds.Height)
        };
        
        return monitor;
    }

    public static Monitor[] GetMonitors()
    {
        List<Monitor> monitors = new List<Monitor>();
        int count = SDL_GetNumVideoDisplays();

        for (int i = 0; i < count; i++)
        {
            Rectangle bounds;
            SDL_GetDisplayBounds(i, &bounds);

            Monitor monitor = new Monitor()
            {
                Position = new Point(bounds.X, bounds.Y),
                Resolution = new Size(bounds.Width, bounds.Height)
            };
            
            monitors.Add(monitor);
        }

        return monitors.ToArray();
    }
}