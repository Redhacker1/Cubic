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
    
    public TextBox(Anchor anchor, Rectangle position, uint textSize = 24, bool captureMouse = true, bool ignoreReferenceResolution = false) : base(anchor, position, captureMouse, ignoreReferenceResolution)
    {
        Text = "";
        _textSize = textSize;
        Input.TextInput += InputOnTextInput;
        _textOffset = 0;
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

        if (Input.KeyPressedOrRepeating(Keys.Left))
            _cursorPos--;
        if (Input.KeyPressedOrRepeating(Keys.Right))
            _cursorPos++;

        _cursorPos = CubicMath.Clamp(_cursorPos, 0, Text.Length);

        Size textSize = UI.Theme.Font.MeasureString(_textSize, Text[.._cursorPos]);
        while (textSize.Width - _textOffset > Position.Width)
            _textOffset++;

        if (textSize.Width - _textOffset < 0)
            _textOffset -= 50;

        if (_textOffset < 0)
            _textOffset = 0;
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);

        graphics.SpriteRenderer.End();

        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution);
        uint textSize = (uint) (_textSize * UI.GetReferenceMultiplier());
        
        graphics.SetScissor(rect);
        graphics.SpriteRenderer.Begin();

        graphics.SpriteRenderer.DrawBorderRectangle(rect.Location.ToVector2(), rect.Size.ToVector2(), UI.Theme.BorderWidth,
            UI.Theme.BorderColor, UI.Theme.RectColor, 0, Vector2.Zero);
        UI.Theme.Font.Draw(graphics.SpriteRenderer, textSize, Text, new Vector2(rect.X - _textOffset, rect.Y + rect.Height / 2), UI.Theme.TextColor,
            0, new Vector2(0, UI.Theme.Font.MeasureString(textSize, Text).Height / 2), Vector2.One);

        graphics.SpriteRenderer.DrawRectangle(
            new Vector2(rect.X + UI.Theme.Font.MeasureString(textSize, Text[.._cursorPos]).Width - _textOffset,
                rect.Y + rect.Height / 2), new Vector2(1, rect.Height - 10), Color.White, 0, new Vector2(0, 0.5f));
        
        graphics.SpriteRenderer.End();
        graphics.SetScissor(graphics.Viewport);
        graphics.SpriteRenderer.Begin();
    }
}