using System.Drawing;

namespace Cubic.Utilities;

public static class Utils
{
    public static Color ColorFromHex(int hexValue)
    {
        return Color.FromArgb(hexValue >> 16, (hexValue >> 8) & 0xFF, hexValue & 0xFF);
    }
}