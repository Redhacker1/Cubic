using System.Drawing;
using System.Numerics;

namespace Cubic.GUI;

public static partial class UI
{
    /// <summary>
    /// Draw a label to the screen.
    /// </summary>
    /// <param name="anchor">The anchor point of this label.</param>
    /// <param name="pos">The offset position from the anchor point.</param>
    /// <param name="text">The text for this label.</param>
    /// <param name="size">The font size of this label.</param>
    /// <param name="captureMouse">If true, (default: false), this label will behave like other UI elements and prevent the item behind it from being clicked.</param>
    public static void Label(Anchor anchor, Point pos, string text, uint size, bool captureMouse = false, bool ignoreReferenceResolution = false, Color? color = null)
    {
        Rectangle rect = new Rectangle(pos, Theme.Font.MeasureString(size, text));
        CalculatePos(anchor, ref rect, ignoreReferenceResolution);
        Add(rect, captureMouse);
        _texts.Add((text, (uint) (size * GetReferenceMultiplier()), new Vector2(rect.X, rect.Y), color ?? Theme.TextColor, false, false, _currentID));
    }
}