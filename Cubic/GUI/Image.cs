using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public class Image : UIElement
{
    public Texture2D Texture;
    public Color Tint;
    
    public Image(Anchor anchor, Rectangle position, Texture2D texture, Color tint = default, bool captureMouse = true,
        bool ignoreReferenceResolution = false, Point? index = null) : base(anchor, position, captureMouse,
        ignoreReferenceResolution, index)
    {
        Texture = texture;
        Tint = tint == default ? Color.White : tint;
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);
        
        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);

        graphics.SpriteRenderer.Draw(Texture, rect.Location.ToVector2(), null, Tint, 0, Vector2.Zero,
            new Vector2(rect.Width / (float) Texture.Size.Width, rect.Height / (float) Texture.Size.Height),
            SpriteFlipMode.None);
    }
}