using System;
using System.Drawing;
using System.Numerics;
using Cubic.Render;

namespace Cubic.GUI;

public static partial class UI
{
    private static Point _textCursorPos;
    private static int _textOffset;
    
    public static bool TextBox(Anchor anchor, Rectangle pos, ref string text, uint size = 24, int maxLength = -1, bool ignoreReferenceResolution = false)
    {
        int borderThickness = Theme.BorderWidth;
        Color borderColor = Theme.BorderColor;
        Color buttonColor = Theme.RectColor;
        
        CalculatePos(anchor, ref pos, ignoreReferenceResolution, Vector2.Zero);
        Add(pos);

        if (ElementClicked(pos))
        {
            _textCursorPos.X = text.Length;
        }

        if (IsFocused())
        {
            foreach (char c in _charBuffer)
            {
                text = text.Insert(_textCursorPos.X, c.ToString());
                _textCursorPos.X++;
            }

            Size texSize = Theme.Font.MeasureString(size, text[.._textCursorPos.X]);

            while (texSize.Width - _textOffset > pos.Width - 5)
                _textOffset += 1;
            while (texSize.Width - _textOffset < 0 + 5)
                _textOffset -= 50;
            if (_textOffset < -5)
                _textOffset = -5;

            if (Input.KeyPressedOrRepeating(Keys.Backspace))
            {
                if (text.Length > 0)
                {
                    text = text.Remove(_textCursorPos.X - 1, 1);
                    _textCursorPos.X--;
                }
            }

            if (Input.KeyPressedOrRepeating(Keys.Left))
            {
                if (_textCursorPos.X > 0)
                    _textCursorPos.X--;
            }

            if (Input.KeyPressedOrRepeating(Keys.Right))
            {
                if (_textCursorPos.X < text.Length)
                    _textCursorPos.X++;
            }

            if (Input.KeyPressed(Keys.Home))
                _textCursorPos.X = 0;
            if (Input.KeyPressed(Keys.End))
                _textCursorPos.X = text.Length;
        }
        
        _rectangles.Add((pos, borderColor, Texture2D.Blank, _currentID));
        _rectangles.Add((
            new Rectangle(pos.X + borderThickness, pos.Y + borderThickness, pos.Width - borderThickness * 2,
                pos.Height - borderThickness * 2), buttonColor, Texture2D.Blank, _currentID));
        if (IsFocused())
        {
            _rectangles.Add((
                new Rectangle(
                    pos.X + 1 - _textOffset +
                    Theme.Font.MeasureString(size, text[.._textCursorPos.X], ignoreParams: true).Width,
                    pos.Y + pos.Height / 2 - (int) size / 2, 1, (int) size), Color.White, Texture2D.Blank, _currentID));
        }

        _texts.Add((text, size, new Vector2(pos.X - _textOffset, pos.Y + pos.Height / 2 - size / 2), Theme.TextColor, false, true, _currentID));

        return ElementClicked(pos);
    }
}