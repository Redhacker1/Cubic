using System;
using System.Drawing;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.Entities.Components;

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

    protected internal override void Draw()
    {
        Transform transform = Transform;
        Graphics.SpriteRenderer.Draw(SpriteTexture, transform.Position.ToVector2(), Source, Tint,
            Transform.Rotation.ToEulerAngles().Z, transform.Origin, transform.Scale.ToVector2(), Flip,
            transform.Position.Z);
    }
}