using System.Drawing;
using System.Numerics;
using Cubic.Render;

namespace Cubic.GUI;

public static partial class UI
{
    /// <summary>
    /// Draw a button to the screen.
    /// </summary>
    /// <param name="anchor">The anchor point of this label.</param>
    /// <param name="pos">The offset position from the anchor point, and the size of the button element itself.</param>
    /// <param name="text">The text (if any) the button will display.</param>
    /// <param name="textSize">The font size (default: 24) of the text on the button.</param>
    /// <returns>True if the button is clicked.</returns>
    public static bool Button(Anchor anchor, Rectangle pos, string text = "", uint textSize = 24, bool ignoreReferenceResolution = false)
    {
        // Get the current UI theme...
        int borderThickness = Theme.BorderWidth;
        Color borderColor = Theme.BorderColor;
        Color buttonColor = Theme.RectColor;

        // Calculate the correct position of the element based on its anchor point.
        CalculatePos(anchor, ref pos, ignoreReferenceResolution);
        Add(pos);

        float scale = GetReferenceMultiplier();
        textSize = (uint) (textSize * scale);
        //borderThickness = (int) (borderThickness * scale);

        if (MouseHovering(pos))
        {
            buttonColor = Theme.HoverColor;
            if (_mouseButtonHeld)
                buttonColor = Theme.ClickColor;
        }

        // Instead of creating a separate "border rectangle" texture like I used to, all we simply do here is draw two
        // rectangles, the first one being the "full size" or "border" rectangle, and the second one being a slightly
        // smaller "actual" rectangle. This rectangle changes colour based on the state of the button. It's FAR more
        // efficient and memory wise than the old system.
        _rectangles.Add((pos, borderColor, Texture2D.Blank, _currentID));
        _rectangles.Add((
            new Rectangle(pos.X + borderThickness, pos.Y + borderThickness, pos.Width - borderThickness * 2,
                pos.Height - borderThickness * 2), buttonColor, Texture2D.Blank, _currentID));
        // The text gets centered around the origin too.
        _texts.Add((text, textSize, new Vector2(pos.X + pos.Width / 2, pos.Y + pos.Height / 2), Theme.TextColor,
            true, false, _currentID));

        return ElementClicked(pos);
    }
}