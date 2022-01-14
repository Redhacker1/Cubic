using System;
using System.Drawing;
using Cubic2D.Render;

namespace Cubic2D.Entities.Components;

public sealed class Sprite : Component
{
    private Texture2D _texture;
    
    public Sprite(Texture2D texture)
    {
        _texture = texture;
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);

        Transform transform = Transform;
        graphics.SpriteRenderer.Draw(_texture, transform.Position, null, Color.White, transform.Rotation,
            transform.Origin, transform.Scale, SpriteFlipMode.None);
    }
}