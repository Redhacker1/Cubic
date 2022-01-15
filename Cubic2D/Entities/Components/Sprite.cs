using System;
using System.Drawing;
using Cubic2D.Render;

namespace Cubic2D.Entities.Components;

public sealed class Sprite : Component
{
    /// <summary>
    /// The texture of this sprite. 
    /// </summary>
    public Texture2D SpriteTexture;
    
    public Sprite(Texture2D spriteTexture)
    {
        SpriteTexture = spriteTexture;
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);

        Transform transform = Transform;
        graphics.SpriteRenderer.Draw(SpriteTexture, transform.Position, null, Color.White, transform.Rotation,
            transform.Origin, transform.Scale, SpriteFlipMode.None);
    }
}