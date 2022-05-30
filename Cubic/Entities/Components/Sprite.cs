using System;
using System.Drawing;
using System.Numerics;
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

    /// <summary>
    /// If false, the sprite will just be the <see cref="Tint"/> colour. It will, however, respect the alpha values of
    /// the sprite's texture. All transparent parts of the texture will remain transparent (or translucent).
    /// </summary>
    public bool UseTexture;

    /// <summary>
    /// The drop shadow used on this sprite, if any.
    /// </summary>
    public DropShadow? Shadow;

    public Sprite(Texture2D spriteTexture)
    {
        SpriteTexture = spriteTexture;
        Tint = Color.White;
        Source = null;
        Flip = SpriteFlipMode.None;
        UseTexture = true;
        Shadow = null;
    }

    protected internal override void Draw()
    {
        Transform transform = Transform;
        Vector2 pos = transform.Position.ToVector2();
        Vector2 scale = transform.Scale.ToVector2();
        float rot = transform.SpriteRotation;

        if (Shadow.HasValue)
        {
            DropShadow sh = Shadow.Value;
            Graphics.SpriteRenderer.Draw(SpriteTexture, pos + sh.Offset, Source,
                Color.FromArgb(sh.Opacity, Color.Black), rot, transform.Origin, scale, Flip, transform.Position.Z,
                false);
        }

        Graphics.SpriteRenderer.Draw(SpriteTexture, pos, Source, Tint, rot, transform.Origin, scale, Flip,
            transform.Position.Z, UseTexture);
    }

    public struct DropShadow
    {
        /// <summary>
        /// The number of pixels the drop shadow should offset by, from the sprite position.
        /// </summary>
        public Vector2 Offset;
        
        /// <summary>
        /// The value, between 0 and 255, that the opacity of the drop shadow should be.
        /// </summary>
        public byte Opacity;
        
        public DropShadow(Vector2 offset, byte opacity)
        {
            Offset = offset;
            Opacity = opacity;
        }
    }
}