using System;
using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public class TextBox : UIElement
{
    public event OnTextChanged TextChanged;
    
    public string Text;
    public string Placeholder;

    private uint _textSize;

    private int _cursorPos;
    private int _textOffset;
    private int _selectionRectBegin;
    private int _selectionRectEnd;

    private bool _blink;
    private float _blinkTimer;
    private const float BlinkTime = 0.5f;
    private bool _wasFocused;

    private const int Padding = 5;
    
    public int MaxLength;
    
    public TextBox(Anchor anchor, Rectangle position, string placeholder = "", uint textSize = 24, bool captureMouse = true, bool ignoreReferenceResolution = false) : base(anchor, position, captureMouse, ignoreReferenceResolution)
    {
        Text = "";
        Placeholder = placeholder;
        _textSize = textSize;
        Input.TextInput += InputOnTextInput;
        _textOffset = 0;
        _selectionRectBegin = 0;
        _selectionRectEnd = 0;
        _blink = true;
        _blinkTimer = BlinkTime;
        MaxLength = int.MaxValue;
    }

    private void InputOnTextInput(char character)
    {
        if (!Focused)
            return;
        
        Text = Text.Insert(_cursorPos, character.ToString());
        TextChanged?.Invoke(Text);
        _blink = true;
        _blinkTimer = BlinkTime;
        _cursorPos++;
    }

    protected internal override void Update(ref bool mouseCaptured)
    {
        base.Update(ref mouseCaptured);

        if (!_wasFocused)
        {
            _blink = true;
            _blinkTimer = BlinkTime;
        }
        
        _wasFocused = Focused;
        
        if (!Focused)
            return;

        if (Input.KeyPressedOrRepeating(Keys.Backspace))
        {
            if (_cursorPos > 0)
            {
                Text = Text.Remove(_cursorPos - 1, 1);
                TextChanged?.Invoke(Text);
                _cursorPos--;
                _blink = true;
                _blinkTimer = BlinkTime;
            }
        }

        if (Input.KeyPressedOrRepeating(Keys.Delete))
        {
            if (_cursorPos < Text.Length)
            {
                Text = Text.Remove(_cursorPos, 1);
                TextChanged?.Invoke(Text);
                _blink = true;
                _blinkTimer = BlinkTime;
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
        {
            _cursorPos--;
            _blink = true;
            _blinkTimer = BlinkTime;
        }

        if (Input.KeyPressedOrRepeating(Keys.Right))
        {
            _cursorPos++;
            _blink = true;
            _blinkTimer = BlinkTime;
        }

        if (Input.KeyPressed(Keys.Home))
        {
            _cursorPos = 0;
            _blink = true;
            _blinkTimer = BlinkTime;
        }

        if (Input.KeyPressed(Keys.End))
        {
            _cursorPos = Text.Length;
            _blink = true;
            _blinkTimer = BlinkTime;
        }

        _cursorPos = CubicMath.Clamp(_cursorPos, 0, Text.Length);
        
        Size measureText = Theme.Font.MeasureString(textSize, Text[.._cursorPos], ignoreParams: true);
        while (measureText.Width - _textOffset > rect.Width - Padding)
            _textOffset += 5;

        while (measureText.Width - _textOffset < Padding)
            _textOffset -= (int) (100 * UI.GetReferenceMultiplier());

        if (_textOffset < Padding)
            _textOffset = -Padding;

        if (Text.Length >= MaxLength)
            Text = Text[..MaxLength];
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
        
        if (!string.IsNullOrEmpty(Placeholder) && Text.Length == 0)
        {
            Theme.Font.Draw(graphics.SpriteRenderer, textSize, Placeholder,
                new Vector2(rect.X + Padding, rect.Y + rect.Height / 2), Theme.AccentTextColor, 0,
                new Vector2(0, textSize / 2), Vector2.One, smartPlacement: false);
        }

        Theme.Font.Draw(graphics.SpriteRenderer, textSize, Text, new Vector2(rect.X - _textOffset, rect.Y + rect.Height / 2), Theme.TextColor,
            0, new Vector2(0, textSize / 2), Vector2.One, ignoreParams: true, smartPlacement: false);

        graphics.SpriteRenderer.End();
        graphics.Scissor = scissor;
        graphics.SpriteRenderer.Begin();

        if (!Focused)
            return;

        _blinkTimer -= Time.DeltaTime;
        if (_blinkTimer <= 0)
        {
            _blink = !_blink;
            _blinkTimer = BlinkTime;
        }
        
        if (!_blink)
            return;

        graphics.SpriteRenderer.DrawRectangle(
            new Vector2(rect.X + Theme.Font.MeasureString(textSize, Text[.._cursorPos], ignoreParams: true).Width - _textOffset,
                rect.Y + rect.Height / 2), new Vector2(1, rect.Height - 10), Theme.TextColor, 0, new Vector2(0, 0.5f));
    }
    
    public delegate void OnTextChanged(string text);
}