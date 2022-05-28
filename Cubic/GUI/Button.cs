using System;
using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public class Button : UIElement
{
    private string _text;
    private uint _fontSize;

    public Button(Anchor anchor, Rectangle position, string text = "", uint fontSize = 24, bool captureMouse = true,
        bool ignoreReferenceResolution = false) : base(anchor, position, captureMouse, ignoreReferenceResolution)
    {
        _text = text;
        _fontSize = fontSize;
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);

        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution);

        Color color = UI.Theme.RectColor;
        if (Hovering)
            color = UI.Theme.HoverColor;
        if (Clicked)
            color = UI.Theme.ClickColor;
        
        graphics.SpriteRenderer.DrawBorderRectangle(rect.Location.ToVector2(), rect.Size.ToVector2(), UI.Theme.BorderWidth,
            UI.Theme.BorderColor, color, 0, Vector2.Zero);

        Size origin = UI.Theme.Font.MeasureString((uint) (_fontSize * UI.GetReferenceMultiplier()), _text);
        
        UI.Theme.Font.Draw(graphics.SpriteRenderer, (uint) (_fontSize * UI.GetReferenceMultiplier()), _text,
            new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), UI.Theme.TextColor, 0,
            new Vector2(origin.Width / 2, origin.Height / 2), Vector2.One);
    }
}