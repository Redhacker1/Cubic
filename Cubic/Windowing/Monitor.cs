using System.Drawing;

namespace Cubic.Windowing;

/// <summary>
/// Represents a physical monitor attached to the device.
/// </summary>
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