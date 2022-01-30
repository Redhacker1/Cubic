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

    /// <summary>
    /// Create a new dynamic font. This font can be retrieved at any font size.
    /// </summary>
    /// <param name="game">The active Cubic game.</param>
    /// <param name="fontPath">The path to the font.</param>
    /// <param name="texWidth">The width of the font atlas's texture. By default, this is 1024.</param>
    /// <param name="texHeight">The height of the font atlas's texture. By default, this is 1024.</param>
    /// <exception cref="CubicException">Thrown if the font does not exist.</exception>
    public Font(CubicGame game, string fontPath, int texWidth = 1024, int texHeight = 1024)
    {
        _game = game;
        if (FT_New_Face(FontHelper.FreeType.Native, fontPath, 0, out _face) != FT_Error.FT_Err_Ok)
            throw new CubicException("Font could not be loaded!");

        _cachedAtlases = new Dictionary<uint, (Texture2D, Dictionary<char, FontHelper.Character>)>();

        _texWidth = texWidth;
        _texHeight = texHeight;
        
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
        // TODO: Implement origin, scale, rotation.
        
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
                _characters = FontHelper.CreateFontTexture(newTex, _game.Graphics, _face, size);
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
            switch (c)
            {
                // If newline argument is given, actually create a new line in our text drawing.
                case '\n':
                    pos.Y += _storedSize + extraLineSpacing;
                    pos.X = position.X;
                    continue;
                case '\\':
                    if (text[i + 1] == '<')
                        continue;
                    break;
                // '<' characters potentially represents commands the renderer can follow.
                case '<':
                    // Backslashes are ignored.
                    if (text[i - 1] == '\\')
                        break;
                    switch (text[(i + 1)..(i + 7)])
                    {
                        // If our given "command" is colour, work out the colour!
                        case "Color:":
                            string col = "";
                            int texPtr = i + 6;
                            while (text[++texPtr] != '>')
                                col += text[texPtr];
                            col = col.Trim();
                            currentColor = FontHelper.GetColor(col);
                            i = texPtr;
                            break;
                    }
                    // This continue is here as without it the first bracket would be drawn.
                    continue;
            }

            FontHelper.Character chr = _characters[c];
            // Calculate the character's position.
            Vector2 charPos =
                new Vector2(pos.X + chr.Bearing.X, pos.Y - chr.Size.Height + (chr.Size.Height - chr.Bearing.Y));
            // The sprite batch takes care of the rest :)
            renderer.Draw(_currentTexture, charPos, new Rectangle(chr.Position, chr.Size), currentColor, rotation, origin,
                scale, SpriteFlipMode.None);
            pos.X += chr.Advance;
        }
    }

    public void Dispose()
    {
        FT_Done_Face(_face);
        foreach (KeyValuePair<uint, (Texture2D, Dictionary<char, FontHelper.Character>)> atlas in _cachedAtlases)
            atlas.Value.Item1.Dispose();
    }
}