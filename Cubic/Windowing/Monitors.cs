using System.Collections.Generic;
using System.Drawing;
using Silk.NET.GLFW;
using static Cubic.Windowing.GameWindow;
using GMonitor = Silk.NET.GLFW.Monitor;

namespace Cubic.Windowing;

public static unsafe class Monitors
{
    /// <summary>
    /// Get the total number of monitors attached to the system.
    /// </summary>
    public static readonly int Count;

    /// <summary>
    /// The primary monitor attached to this system.
    /// </summary>
    public static readonly Monitor PrimaryMonitor;

    /// <summary>
    /// Get all monitors attached to the system. AttachedMonitors[0] == PrimaryMonitor.
    /// </summary>
    public static readonly Monitor[] AttachedMonitors;
    
    static Monitors()
    {
        GMonitor** gMonitors = GLFW.GetMonitors(out Count);

        List<Monitor> monitors = new List<Monitor>();
        for (int i = 0; i < Count; i++)
        {
            GMonitor* m = gMonitors[i];
            VideoMode* mode = GLFW.GetVideoMode(m);
            GLFW.GetMonitorPos(m, out int x, out int y);

            List<DisplayMode> displayModes = new List<DisplayMode>();
            VideoMode* modes = GLFW.GetVideoModes(m, out int count);
            for (int dm = 0; dm < count; dm++)
            {
                displayModes.Add(new DisplayMode()
                {
                    Resolution = new Size(modes[dm].Width, modes[dm].Height),
                    RefreshRate = modes[dm].RefreshRate
                });
            }

            Monitor monitor = new Monitor()
            {
                Position = new Point(x, y),
                CurrentDisplayMode = new DisplayMode()
                {
                    Resolution = new Size(mode->Width, mode->Height),
                    RefreshRate = mode->RefreshRate
                },
                AvailableDisplayModes = displayModes.ToArray()
            };
            monitors.Add(monitor);
        }

        AttachedMonitors = monitors.ToArray();

        PrimaryMonitor = monitors[0];
    }
}