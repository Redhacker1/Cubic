using System.Drawing;

namespace Cubic.GUI;

public static partial class UI
{
    public static void Rectangle(Anchor anchor, Rectangle pos, Color color, bool captureMouse = true)
    {
        CalculatePos(anchor, ref pos);
        AddElement(pos, captureMouse);
        _rectangles.Add((pos, color, _currentID));
    }
}