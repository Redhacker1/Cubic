using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using Cubic2D.Scenes;
using Cubic2D.Windowing;
using FreeTypeSharp;
using FreeTypeSharp.Native;
using static FreeTypeSharp.Native.FT;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Cubic2D.Render.Text;

public class Font : IDisposable
{
    private IntPtr _face;
    private Texture2D _currentTexture;
    private CubicGame _game;

    private Dictionary<uint, (Texture2D, Dictionary<char, FontHelper.Character>)> _cachedAtlases;

    private uint _storedSize;

    private Dictionary<char, FontHelper.Character> _characters;

    private readonly int _texWidth;
    private readonly int _texHeight;

    private readonly uint _unicodeRangeStart;
    private readonly uint _unicodeRangeEnd;

    /// <summary>
    /// Create a new dynamic font. This font can be retrieved at any font size.
    /// </summary>
    /// <param name="game">The active Cubic game.</param>
    /// <param name="fontPath">The path to the font.</param>
    /// <param name="unicodeRangeStart">The starting character of the unicode set.</param>
    /// <param name="unicodeRangeEnd">The ending character of the unicode set.</param>
    /// <param name="texWidth">The width of the font atlas's texture. By default, this is 1024.</param>
    /// <param name="texHeight">The height of the font atlas's texture. By default, this is 1024.</param>
    /// <exception cref="CubicException">Thrown if the font does not exist.</exception>
    public Font(CubicGame game, string fontPath, uint unicodeRangeStart = 0, uint unicodeRangeEnd = 128,
        int texWidth = 1024, int texHeight = 1024)
    {
        _game = game;
        if (FT_New_Face(FontHelper.FreeType.Native, fontPath, 0, out _face) != FT_Error.FT_Err_Ok)
            throw new CubicException("Font could not be loaded!");

        _cachedAtlases = new Dictionary<uint, (Texture2D, Dictionary<char, FontHelper.Character>)>();

        _texWidth = texWidth;
        _texHeight = texHeight;

        _unicodeRangeStart = unicodeRangeStart;
        _unicodeRangeEnd = unicodeRangeEnd;
        
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
    /// <param name="extraLineSpacing">Any additional spacing between lines. This can also be a negative number if you want less spacing.</param>
    /// <exception cref="CubicException">Thrown if an incorrect parameter is given to the text drawer.</exception>
    public void Draw(SpriteRenderer renderer, uint size, string text, Vector2 position, Color startColor,
        float rotation, Vector2 origin, Vector2 scale, int extraLineSpacing = 0)
    {
        // We need to keep a reference to both the current character's position and our actual position, which is what
        // we do here.
        Vector2 pos = position;
        
        // If the font size is not the same as our old one, generate a new texture!
        if (size != _storedSize)
        {
            if (_cachedAtlases.ContainsKey(size))
            {
                (Texture2D, Dictionary<char, FontHelper.Character>) ch = _cachedAtlases[size];
                _currentTexture = ch.Item1;
                _characters = ch.Item2;
            }
            else
            {
                Texture2D newTex = new Texture2D(_game, _texWidth, _texHeight);
                _currentTexture = newTex;
                _characters = FontHelper.CreateFontTexture(newTex, _game.Graphics, _face, size, _unicodeRangeStart,
                    _unicodeRangeEnd);
                _cachedAtlases.Add(size, (newTex, _characters));
            }
            _storedSize = size;
        }

        // Calculate the largest character in height in our text. This is used to ensure the text is drawn at the
        // correct position.
        int largestChar = 0;
        foreach (char c in text)
        {
            FontHelper.Character chr = _characters[c];
            if (chr.Bearing.Y > largestChar)
                largestChar = chr.Bearing.Y;
        }
        pos.Y += largestChar;

        // Loop through each character in our text :)
        Color currentColor = startColor;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            FontHelper.StringParam param = FontHelper.CheckParams(ref c, ref i, text);
            switch (param.Type)
            {
                case FontHelper.ParamType.NewLine:
                    pos.Y += _storedSize + extraLineSpacing;
                    pos.X = position.X;
                    continue;
                case FontHelper.ParamType.EscapeChar:
                    continue;
                case FontHelper.ParamType.Color:
                    currentColor = param.Color;
                    continue;
            }

            FontHelper.Character chr = _characters[c];
            // Calculate the character's position.
            Vector2 charPos =
                new Vector2(pos.X + chr.Bearing.X, pos.Y - chr.Size.Height + (chr.Size.Height - chr.Bearing.Y));
            // First we translate the char position by our rotation, scale, and origin point, then the sprite batch
            // takes care of the rest :)
            charPos = Vector2.Transform(charPos,
                Matrix4x4.CreateTranslation(new Vector3(-origin - position, 0)) *
                Matrix4x4.CreateScale(new Vector3(scale, 1)) * Matrix4x4.CreateRotationZ(rotation) *
                Matrix4x4.CreateTranslation(new Vector3(position, 0)));

            renderer.Draw(_currentTexture, charPos, new Rectangle(chr.Position, chr.Size), currentColor, rotation,
                Vector2.Zero, scale, SpriteFlipMode.None);
            pos.X += chr.Advance;
        }
    }

    /// <summary>
    /// Roughly measure the displayed size in pixels of a text string, with this font.
    /// </summary>
    /// <param name="size">The font size.</param>
    /// <param name="text">The text to measure.</param>
    /// <param name="extraLineSpacing">Any extra line spacing between lines.</param>
    /// <returns></returns>
    public Size MeasureString(uint size, string text, int extraLineSpacing = 0)
    {
        // A fair bit of code here is reused from Draw(). We even generate a new texture... (Although to be
        // honest, who would measure the size of a string in a font size that is never going to be used?)
        
        Size stringSize = new Size(0, (int) size);

        // If the font size is not the same as our old one, generate a new texture!
        if (size != _storedSize)
        {
            if (_cachedAtlases.ContainsKey(size))
            {
                (Texture2D, Dictionary<char, FontHelper.Character>) ch = _cachedAtlases[size];
                _currentTexture = ch.Item1;
                _characters = ch.Item2;
            }
            else
            {
                Texture2D newTex = new Texture2D(_game, _texWidth, _texHeight);
                _currentTexture = newTex;
                _characters = FontHelper.CreateFontTexture(newTex, _game.Graphics, _face, size, _unicodeRangeStart,
                    _unicodeRangeEnd);
                _cachedAtlases.Add(size, (newTex, _characters));
            }
            _storedSize = size;
        }

        int pos = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            
            // We need to include this code here too to make sure the measurestring doesn't include the parameters
            // as part of the text, otherwise the size would be very much off.
            FontHelper.StringParam param = FontHelper.CheckParams(ref c, ref i, text);
            switch (param.Type)
            {
                case FontHelper.ParamType.NewLine:
                    stringSize.Height += (int) _storedSize + extraLineSpacing;
                    pos = 0;
                    continue;
                case FontHelper.ParamType.EscapeChar:
                case FontHelper.ParamType.Color:
                    continue;
            }

            FontHelper.Character chr = _characters[c];
            Vector2 charPos =
                new Vector2(pos + chr.Bearing.X, chr.Size.Height + (chr.Size.Height - chr.Bearing.Y));
            if ((int) charPos.X + chr.Size.Width > stringSize.Width)
                stringSize.Width = (int) charPos.X + chr.Size.Width;
            pos += chr.Advance;
        }

        return stringSize;
    }

    public void Dispose()
    {
        FT_Done_Face(_face);
        foreach (KeyValuePair<uint, (Texture2D, Dictionary<char, FontHelper.Character>)> atlas in _cachedAtlases)
            atlas.Value.Item1.Dispose();
    }
}