using System;
using System.Drawing;

namespace Cubic.Windowing;

public struct DisplayMode : IEquatable<DisplayMode>
{
    /// <summary>
    /// The resolution of this display mode in pixels.
    /// </summary>
    public Size Resolution;

    /// <summary>
    /// The refresh rate of this display mode in Hz.
    /// </summary>
    public int RefreshRate;

    public bool Equals(DisplayMode other)
    {
        return Resolution.Equals(other.Resolution) && RefreshRate == other.RefreshRate;
    }

    public override bool Equals(object obj)
    {
        return obj is DisplayMode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Resolution, RefreshRate);
    }

    public static bool operator ==(DisplayMode disp1, DisplayMode disp2)
    {
        return disp1.Equals(disp2);
    }

    public static bool operator !=(DisplayMode disp1, DisplayMode disp2)
    {
        return !(disp1 == disp2);
    }
}