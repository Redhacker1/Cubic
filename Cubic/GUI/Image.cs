using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public class Image : UIElement
{
    private Texture2D _texture;
    private Color _tint;
    
    public Image(Anchor anchor, Rectangle position, Texture2D texture, Color tint = default, bool captureMouse = true,
        bool ignoreReferenceResolution = false, Point? index = null) : base(anchor, position, captureMouse,
        ignoreReferenceResolution, index)
    {
        _texture = texture;
        _tint = tint == default ? Color.White : tint;
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);
        
        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);

        graphics.SpriteRenderer.Draw(_texture, rect.Location.ToVector2(), null, _tint, 0, Vector2.Zero,
            new Vector2(rect.Width / (float) _texture.Size.Width, rect.Height / (float) _texture.Size.Height),
            SpriteFlipMode.None);
    }
}