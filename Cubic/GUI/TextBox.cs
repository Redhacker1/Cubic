using System;
using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public class TextBox : UIElement
{
    public string Text;

    private uint _textSize;

    private int _cursorPos;
    private int _textOffset;
    private int _selectionRectBegin;
    private int _selectionRectEnd;

    private const int Padding = 5;
    
    public TextBox(Anchor anchor, Rectangle position, uint textSize = 24, bool captureMouse = true, bool ignoreReferenceResolution = false) : base(anchor, position, captureMouse, ignoreReferenceResolution)
    {
        Text = "";
        _textSize = textSize;
        Input.TextInput += InputOnTextInput;
        _textOffset = 0;
        _selectionRectBegin = 0;
        _selectionRectEnd = 0;
    }

    private void InputOnTextInput(char character)
    {
        Text = Text.Insert(_cursorPos, character.ToString());
        _cursorPos++;
    }

    protected internal override void Update(ref bool mouseCaptured)
    {
        base.Update(ref mouseCaptured);

        if (Input.KeyPressedOrRepeating(Keys.Backspace))
        {
            if (_cursorPos > 0)
            {
                Text = Text.Remove(_cursorPos - 1, 1);
                _cursorPos--;
            }
        }

        if (Input.KeyPressedOrRepeating(Keys.Delete))
        {
            if (_cursorPos < Text.Length)
            {
                Text = Text.Remove(_cursorPos, 1);
            }
        }

        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);
        uint textSize = (uint) (_textSize * UI.GetReferenceMultiplier());

        if (Clicked)
        {
            int clickPoint = ClickPoint.X + _textOffset;
            int lastWidth = 0;
            for (int i = 0; i < Text.Length + 1; i++)
            {
                int width = Theme.Font.MeasureString(textSize, Text[..i], ignoreParams: true).Width;

                if (width - (width - lastWidth) / 2f >= clickPoint)
                {
                    _cursorPos = i - 1;
                    break;
                }
                else
                    _cursorPos = i;

                lastWidth = width;
            }
        }
        
        if (Input.KeyPressedOrRepeating(Keys.Left))
            _cursorPos--;
        if (Input.KeyPressedOrRepeating(Keys.Right))
            _cursorPos++;

        if (Input.KeyPressed(Keys.Home))
            _cursorPos = 0;
        if (Input.KeyPressed(Keys.End))
            _cursorPos = Text.Length;

        _cursorPos = CubicMath.Clamp(_cursorPos, 0, Text.Length);
        
        Size measureText = Theme.Font.MeasureString(textSize, Text[.._cursorPos], ignoreParams: true);
        while (measureText.Width - _textOffset > rect.Width - Padding)
            _textOffset += 5;

        while (measureText.Width - _textOffset < Padding)
            _textOffset -= (int) (100 * UI.GetReferenceMultiplier());

        if (_textOffset < Padding)
            _textOffset = -Padding;
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);
        
        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);
        uint textSize = (uint) (_textSize * UI.GetReferenceMultiplier());
        
        _cursorPos = CubicMath.Clamp(_cursorPos, 0, Text.Length);
        
        graphics.SpriteRenderer.DrawBorderRectangle(rect.Location.ToVector2(), rect.Size.ToVector2(), Theme.BorderWidth,
            Theme.BorderColor, Theme.RectColor, 0, Vector2.Zero);

        graphics.SpriteRenderer.End();

        Rectangle scissor = graphics.Scissor;
        graphics.Scissor = rect with { Y = scissor.Y };
        graphics.SpriteRenderer.Begin();
        
        Theme.Font.Draw(graphics.SpriteRenderer, textSize, Text, new Vector2(rect.X - _textOffset, rect.Y + rect.Height / 2), UI.Theme.TextColor,
            0, new Vector2(0, Theme.Font.MeasureString(textSize, Text, ignoreParams: true).Height / 2), Vector2.One, ignoreParams: true);

        graphics.SpriteRenderer.End();
        graphics.Scissor = scissor;
        graphics.SpriteRenderer.Begin();
        
        graphics.SpriteRenderer.DrawRectangle(
            new Vector2(rect.X + Theme.Font.MeasureString(textSize, Text[.._cursorPos], ignoreParams: true).Width - _textOffset,
                rect.Y + rect.Height / 2), new Vector2(1, rect.Height - 10), Color.White, 0, new Vector2(0, 0.5f));
    }
}