using System.Collections.Generic;
using System.Drawing;
using OpenTK.Windowing.GraphicsLibraryFramework;
using GMonitor = OpenTK.Windowing.GraphicsLibraryFramework.Monitor;

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
        Count = GLFW.GetMonitors().Length;

        List<Monitor> monitors = new List<Monitor>();
        for (int i = 0; i < Count; i++)
        {
            GMonitor* m = GLFW.GetMonitors()[i];
            VideoMode* mode = GLFW.GetVideoMode(m);
            GLFW.GetMonitorPos(m, out int x, out int y);

            List<DisplayMode> displayModes = new List<DisplayMode>();
            foreach (VideoMode md in GLFW.GetVideoModes(m))
            {
                displayModes.Add(new DisplayMode()
                {
                    Resolution = new Size(md.Width, md.Height),
                    RefreshRate = md.RefreshRate
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