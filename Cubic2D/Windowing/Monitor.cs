using System.Drawing;
using Point = Veldrid.Point;

namespace Cubic2D.Windowing;

public struct Monitor
{
    /// <summary>
    /// The resolution, in pixels, of this monitor.
    /// </summary>
    public Size Resolution;
    
    /// <summary>
    /// The position, in pixels, of this monitor. This value is decided by the operating system.
    /// </summary>
    public Point Position;

    public override string ToString()
    {
        return $"Resolution: {Resolution.Width}x{Resolution.Height}, Position: (X: {Position.X}, Y: {Position.Y})";
    }
}