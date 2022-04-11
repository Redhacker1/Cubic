using System.Collections.Generic;
using System.Drawing;
using OpenTK.Windowing.GraphicsLibraryFramework;
using GMonitor = OpenTK.Windowing.GraphicsLibraryFramework.Monitor;

namespace Cubic.Windowing;

public static unsafe class Monitors
{
    public static int Count => GLFW.GetMonitors().Length;
    
    public static Monitor GetPrimaryMonitor() => GetMonitor(0);

    public static Monitor GetMonitor(int index)
    {
        GMonitor* m = GLFW.GetMonitors()[index];
        VideoMode* mode = GLFW.GetVideoMode(m);
        GLFW.GetMonitorPos(m, out int x, out int y);

        Monitor monitor = new Monitor()
        {
            Position = new Point(x, y),
            Resolution = new Size(mode->Width, mode->Height)
        };
        
        return monitor;
    }

    public static Monitor[] GetMonitors()
    {
        List<Monitor> monitors = new List<Monitor>();

        foreach (GMonitor* m in GLFW.GetMonitors())
        {
            VideoMode* mode = GLFW.GetVideoMode(m);
            GLFW.GetMonitorPos(m, out int x, out int y);

            Monitor monitor = new Monitor()
            {
                Position = new Point(x, y),
                Resolution = new Size(mode->Width, mode->Height)
            };
            
            monitors.Add(monitor);
        }

        return monitors.ToArray();
    }
}