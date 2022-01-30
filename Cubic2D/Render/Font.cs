using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using Cubic2D.Scenes;
using Cubic2D.Windowing;
using FreeTypeSharp;
using FreeTypeSharp.Native;
using static FreeTypeSharp.Native.FT;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Cubic2D.Render;

public class Font : IDisposable
{
    private FreeTypeLibrary _freeType;
    private IntPtr _face;
    private Texture2D _texture;
    private Graphics _graphics;
    
    private uint _offsetX;
    private uint _offsetY;

    private Dictionary<char, Character> _characters;

    private uint _storedSize;

    /// <summary>
    /// Create a new dynamic font. This font can be retrieved at any font size.
    /// </summary>
    /// <param name="game">The active Cubic game.</param>
    /// <param name="fontPath">The path to the font.</param>
    /// <param name="texWidth">The width of the font atlas's texture. By default, this is 1024.</param>
    /// <param name="texHeight">The height of the font atlas's texture. By default, this is 1024.</param>
    /// <exception cref="CubicException">Thrown if freetype has issues initialising.</exception>
    public Font(CubicGame game, string fontPath, int texWidth = 1024, int texHeight = 1024)
    {
        _graphics = game.Graphics;
        _freeType = new FreeTypeLibrary();
        _characters = new Dictionary<char, Character>();
        if (FT_New_Face(_freeType.Native, fontPath, 0, out _face) != FT_Error.FT_Err_Ok)
            throw new CubicException("Font could not be loaded!");

        _texture = new Texture2D(game, texWidth, texHeight);
        
        SceneManager.Active.CreatedResources.Add(this);
    }
    
    /// <summary>
    /// Draw the text to the screen.
    /// </summary>
    /// <param name="renderer">The sprite renderer that should be used to draw this text.</param>
    /// <param name="size">The font size (height in pixels) this font should be.</param>
    /// <param name="text">The text that should be drawn.</param>
    /// <param name="position">The position the text is drawn at. This will be potentially offset by <paramref name="origin"/> if set.</param>
    /// <param name="startColor">The initial colour of the text. This can be changed during text drawing using <code>&lt;Color: COLOR_NAME&gt;</code></param>
    /// <param name="rotation">The rotation of the text.</param>
    /// <param name="origin">The origin point of the text, scaling and rotation will occur around this point.</param>
    /// <param name="scale">The scale of the text.</param>
    /// <exception cref="CubicException">Thrown if an incorrect parameter is given to the text drawer.</exception>
    public void Draw(SpriteRenderer renderer, uint size, string text, Vector2 position, Color startColor,
        float rotation, Vector2 origin, Vector2 scale)
    {
        // TODO: Implement origin, scale, rotation.
        
        // We need to keep a reference to both the current character's position and our actual position, which is what
        // we do here.
        Vector2 pos = position;
        
        // If the font size is not the same as our old one, generate a new texture!
        if (size != _storedSize)
        {
            CreateFontTexture(size);
            _storedSize = size;
        }

        // Calculate the largest character in height in our text. This is used to ensure the text is drawn at the
        // correct position.
        int largestChar = 0;
        foreach (char c in text)
        {
            Character chr = _characters[c];
            if (chr.Bearing.Y > largestChar)
                largestChar = chr.Bearing.Y;
        }
        pos.Y += largestChar;

        // Loop through each character in our text :)
        Color currentColor = startColor;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            switch (c)
            {
                // If newline argument is given, actually create a new line in our text drawing.
                case '\n':
                    pos.Y += _storedSize;
                    pos.X = position.X;
                    continue;
                // '<' characters potentially represents commands the renderer can follow.
                case '<':
                    // Backslashes are ignored.
                    // TODO: Work out what to do here as currently the backslash is also drawn, which is not ideal behaviour
                    if (text[i - 1] == '\\')
                        continue;
                    switch (text[(i + 1)..(i + 7)])
                    {
                        // If our given "command" is colour, work out the colour!
                        case "Color:":
                            string col = "";
                            int texPtr = i + 6;
                            while (text[++texPtr] != '>')
                                col += text[texPtr];
                            col = col.Trim();
                            // I hate reflection every frame but sadly it is the only way I know how. Probably some way
                            // better but it works for now.
                            PropertyInfo inf =
                                typeof(Color).GetProperty(col, BindingFlags.Public | BindingFlags.Static);
                            if (inf == null)
                                throw new CubicException("Given color is not a valid color.");
                            currentColor = (Color) inf.GetValue(null, null)!;
                            i = texPtr;
                            break;
                    }
                    // This continue is here as without it the first bracket would be drawn.
                    continue;
            }

            Character chr = _characters[c];
            // Calculate the character's position.
            Vector2 charPos =
                new Vector2(pos.X + chr.Bearing.X, pos.Y - chr.Size.Height + (chr.Size.Height - chr.Bearing.Y));
            // The sprite batch takes care of the rest :)
            renderer.Draw(_texture, charPos, new Rectangle(chr.Position, chr.Size), currentColor, rotation, origin,
                scale, SpriteFlipMode.None);
            pos.X += chr.Advance;
        }
    }

    private unsafe void CreateFontTexture(uint fontSize)
    {
        _characters.Clear();
        FT_Set_Pixel_Sizes(_face, 0, fontSize);

        FreeTypeFaceFacade facade = new FreeTypeFaceFacade(_freeType, _face);

        // First we run through and gather metrics on each character, generating a texture for the first 128 ASCII
        // characters
        for (uint c = 0; c < 128; c++)
        {
            FT_Load_Char(_face, c, FT_LOAD_RENDER);

            // Calculate our character's offset based on the last glyph's size.
            // Here, we wrap the glyph to the next "line" of the texture if it overflows the texture's size.
            FT_Bitmap glyph = facade.GlyphBitmap;
            if (_offsetX + glyph.width >= _texture.Size.Width)
            {
                _offsetY += fontSize;
                _offsetX = 0;
            }

            // Ooh boy, pointer garbage!
            // FreeType provides us a font in grayscale, in this case typically the "red" channel if you will.
            // Problem is we can't really draw this. So we need to convert it into RGBA colour space.
            // This garbage does this.
            // First calculate the grayscale size of the glyph. Easy enough, just the width x height (fontsize),
            // multiplied by 8 (as each char is 8 bits).
            uint size = glyph.width * fontSize * 8;
            int offset = 0;
            // We need to create an array that is 4x this size, as we need 4 channels while the current data only has
            // one channel.
            byte[] data = new byte[size * 4];
            // Convert the glyph's buffer to a byte ptr.
            byte* buf = (byte*) glyph.buffer.ToPointer();
            // We repeat this 4 times for the 4 different channels.
            for (int n = 0; n < 4; n++)
            {
                // currOffset stores which byte of the glyph we want.
                int currOffset = 0;
                // Set the R, G, and B channels of the glyph to 255 each, as they just need to be white.
                // The alpha channel is set to the value of the native glyph, producing the result we want.
                for (int x = 0; x < glyph.width; x++)
                {
                    for (int y = 0; y < fontSize; y++)
                    {
                        data[offset++] = 255;
                        data[offset++] = 255;
                        data[offset++] = 255;
                        data[offset++] = buf[currOffset++];
                    }
                }
            }

            // Convert to intptr and update our texture accordingly.
            fixed (byte* p = data)
            {
                _texture.SetData(_graphics, (IntPtr) p, size * 4, _offsetX, _offsetY, glyph.width,
                    fontSize);
            }

            // Load each character into the character dictionary so it can be referenced later.
            Character chr = new Character()
            {
                Position = new Point((int) _offsetX, (int) _offsetY),
                Size = new Size((int) facade.GlyphBitmap.width, (int) facade.GlyphBitmap.rows),
                Bearing = new Point(facade.GlyphBitmapLeft, facade.GlyphBitmapTop),
                Advance = facade.GlyphMetricHorizontalAdvance
            };
            _characters.Add((char) c, chr);

            _offsetX += glyph.width;
        }

        _offsetX = 0;
        _offsetY = 0;
    }

    public void Dispose()
    {
        FT_Done_Face(_face);
        _freeType.Dispose();
        _texture.Dispose();
    }

    private struct Character
    {
        public Point Position;
        public Size Size;
        public Point Bearing;
        public int Advance;
    }
}