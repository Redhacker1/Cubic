using System;
using System.Collections.Generic;
using System.Drawing;
using Cubic2D.Render;
using Cubic2D.Scenes;
using Cubic2D.Windowing;

namespace Cubic2D.Entities.Components;

public sealed class Sprite : Component
{
    /// <summary>
    /// The texture of this sprite. 
    /// </summary>
    public Texture2D SpriteTexture;

    /// <summary>
    /// The tint of this sprite. Changing this will change the sprite's colour. Use a White tint to return the sprite
    /// back to its original colour.
    /// </summary>
    public Color Tint;
    
    /// <summary>
    /// Source rectangle for this sprite. Use if the given <see cref="SpriteTexture"/> is a spritesheet.
    /// </summary>
    public Rectangle? Source;
    
    /// <summary>
    /// The flip mode of this sprite.
    /// </summary>
    public SpriteFlipMode Flip;

    public Sprite(Texture2D spriteTexture)
    {
        SpriteTexture = spriteTexture;
        Tint = Color.White;
        Source = null;
        Flip = SpriteFlipMode.None;
    }

    protected internal override void Draw(Graphics graphics)
    {
        Transform transform = Transform;
        graphics.SpriteRenderer.Draw(SpriteTexture, transform.Position, Source, Tint, transform.Rotation,
            transform.Origin, transform.Scale, Flip, transform.Depth);
    }
}