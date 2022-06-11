using System;
using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public class Button : UIElement
{
    public string Text;
    private uint _fontSize;

    public Texture2D Texture;
    public Size TextureSize;
    public Point TextureOffset;
    public Point TextOffset;

    public Button(Anchor anchor, Rectangle position, string text = "", uint fontSize = 24, bool captureMouse = true,
        bool ignoreReferenceResolution = false) : base(anchor, position, captureMouse, ignoreReferenceResolution)
    {
        Text = text;
        _fontSize = fontSize;
        Texture = null;
        TextureSize = position.Size;
        TextOffset = Point.Empty;
        TextureOffset = Point.Empty;
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);

        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);

        Color color = Theme.RectColor;
        if (Hovering)
            color = Theme.HoverColor;
        if (Clicked)
            color = Theme.ClickColor;
        
        graphics.SpriteRenderer.DrawBorderRectangle(rect.Location.ToVector2(), rect.Size.ToVector2(), Theme.BorderWidth,
            Theme.BorderColor, color, 0, Vector2.Zero);

        float scale = UI.GetReferenceMultiplier();
        
        if (Texture != null)
        {
            graphics.SpriteRenderer.Draw(Texture, rect.Location.ToVector2() + TextureOffset.ToVector2() * scale, null,
                Color.White, 0, Vector2.Zero,
                new Vector2(TextureSize.Width / (float) Texture.Size.Width * scale,
                    TextureSize.Height / (float) Texture.Size.Height * scale), SpriteFlipMode.None);
        }
        
        Size origin = Theme.Font.MeasureString((uint) (_fontSize * scale), Text);

        Theme.Font.Draw(graphics.SpriteRenderer, (uint) (_fontSize * scale), Text,
            new Vector2(rect.X + rect.Width / 2 + (int) (TextOffset.X * scale),
                rect.Y + rect.Height / 2 + (int) (TextOffset.Y * scale)), Theme.TextColor, 0,
            new Vector2(origin.Width / 2, origin.Height / 2), Vector2.One);
    }
}