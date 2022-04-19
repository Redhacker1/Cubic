using System.Drawing;

namespace Cubic.GUI;

public static partial class UI
{
    public static void Rectangle(Anchor anchor, Rectangle pos, Color color, bool captureMouse = true, bool ignoreReferenceResolution = false)
    {
        CalculatePos(anchor, ref pos, ignoreReferenceResolution);
        AddElement(pos, captureMouse);
        _rectangles.Add((pos, color, _currentID));
    }
}