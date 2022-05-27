using System;
using System.Drawing;
using Cubic.Render;

namespace Cubic.GUI;

public static partial class UI
{
    public static bool Rectangle(Anchor anchor, Rectangle pos, Color color, bool captureMouse = true, bool ignoreReferenceResolution = false)
    {
        CalculatePos(anchor, ref pos, ignoreReferenceResolution);
        Add(pos, captureMouse);
        _rectangles.Add((pos, color, Texture2D.Blank, _currentID));
        return MouseHovering(pos);
    }
    
    public static bool Image(Anchor anchor, Rectangle pos, Texture2D texture, bool captureMouse = true, bool ignoreReferenceResolution = false)
    {
        CalculatePos(anchor, ref pos, ignoreReferenceResolution);
        Add(pos, captureMouse);
        _rectangles.Add((pos, Color.White, texture, _currentID));
        return MouseHovering(pos);
    }
}