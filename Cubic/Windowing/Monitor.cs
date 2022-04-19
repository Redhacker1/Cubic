using System.Drawing;

namespace Cubic.Windowing;

/// <summary>
/// Represents a physical monitor attached to the device.
/// </summary>
public struct Monitor
{
    /// <summary>
    /// The current display mode of this monitor.
    /// </summary>
    public DisplayMode CurrentDisplayMode;

    public DisplayMode[] AvailableDisplayModes;
    
    /// <summary>
    /// The position, in pixels, of this monitor. This value is decided by the operating system.
    /// </summary>
    public Point Position;

    public override string ToString()
    {
        return $"Resolution: {CurrentDisplayMode.Resolution.Width}x{CurrentDisplayMode.Resolution.Height}@{CurrentDisplayMode.RefreshRate}Hz, Position: (X: {Position.X}, Y: {Position.Y})";
    }
}