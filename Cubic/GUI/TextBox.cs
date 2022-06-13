using System;
using System.Collections.Generic;
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

    private List<UndoState> _undoStates;
    private int _undoIndex;

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

    private int _lastCursorPos;
    private int _initialCursorPos;
    private bool _textInserted;
    
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
        _lastCursorPos = -1;
        _undoStates = new List<UndoState>();
        _undoIndex = 0;
    }

    private void InputOnTextInput(char character)
    {
        if (!Focused)
            return;

        AdjustForSelection();
        Text = Text.Insert(_cursorPos, character.ToString());
        TextChanged?.Invoke(Text);
        ResetSelection();
        _blink = true;
        _blinkTimer = BlinkTime;
        _cursorPos++;
        _textInserted = true;
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
            if (!AdjustForSelection() && _cursorPos > 0)
            {
                Text = Text.Remove(_cursorPos - 1, 1);
                _cursorPos--;
            }

            TextChanged?.Invoke(Text);
            ResetSelection();
            _blink = true;
            _blinkTimer = BlinkTime;
        }

        if (Input.KeyPressedOrRepeating(Keys.Delete))
        {
            if (_cursorPos < Text.Length)
            {
                if (!AdjustForSelection())
                    Text = Text.Remove(_cursorPos, 1);
                TextChanged?.Invoke(Text);
                ResetSelection();
                _blink = true;
                _blinkTimer = BlinkTime;
            }
        }

        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);
        uint textSize = (uint) (_textSize * UI.GetReferenceMultiplier());

        if (Input.KeyPressedOrRepeating(Keys.Left))
        {
            _cursorPos--;
            if (Input.KeyReleased(Keys.LeftShift))
                ResetSelection();
            _blink = true;
            _blinkTimer = BlinkTime;
        }

        if (Input.KeyPressedOrRepeating(Keys.Right))
        {
            _cursorPos++;
            if (Input.KeyReleased(Keys.LeftShift))
                ResetSelection();
            _blink = true;
            _blinkTimer = BlinkTime;
        }
        
        if (Clicked || Input.KeyDown(Keys.LeftShift) && !_textInserted)
        {
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

            if (_lastCursorPos != -1)
            {
                if (_initialCursorPos == -1)
                {
                    _initialCursorPos = _cursorPos;
                    _selectionRectBegin = _cursorPos;
                    _selectionRectEnd = _cursorPos;
                }

                if (_cursorPos > _lastCursorPos)
                {
                    if (_selectionRectBegin < _initialCursorPos)
                        _selectionRectBegin = _cursorPos;
                    else
                        _selectionRectEnd = _cursorPos;
                }
                else if (_cursorPos < _lastCursorPos)
                {
                    if (_selectionRectEnd > _initialCursorPos)
                        _selectionRectEnd = _cursorPos;
                    else
                        _selectionRectBegin = _cursorPos;
                }
            }

            _lastCursorPos = _cursorPos;
        }
        else
        {
            _lastCursorPos = -1;
            _initialCursorPos = -1;
        }

        if (Input.KeyPressed(Keys.Home))
        {
            _cursorPos = 0;
            ResetSelection();
            _blink = true;
            _blinkTimer = BlinkTime;
        }

        if (Input.KeyPressed(Keys.End))
        {
            _cursorPos = Text.Length;
            ResetSelection();
            _blink = true;
            _blinkTimer = BlinkTime;
        }

        if (Input.KeyDown(Keys.LeftControl))
        {
            if (Input.KeyPressed(Keys.A))
            {
                _selectionRectBegin = 0;
                _selectionRectEnd = Text.Length;
                _cursorPos = Text.Length;
                _blink = true;
                _blinkTimer = BlinkTime;
            }

            if (Input.KeyPressed(Keys.V))
            {
                if (!AdjustForSelection())
                    AddUndo();
                ResetSelection();
                string clipboard = Input.Clipboard.Replace("\n", "");
                Text = Text.Insert(_cursorPos, clipboard);
                _cursorPos += clipboard.Length;
            }

            if (Input.KeyPressed(Keys.C))
            {
                if (_selectionRectEnd - _selectionRectBegin > 0)
                    Input.Clipboard = Text[_selectionRectBegin.._selectionRectEnd];
            }

            if (Input.KeyPressed(Keys.X))
            {
                if (_selectionRectEnd - _selectionRectBegin > 0)
                {
                    Input.Clipboard = Text[_selectionRectBegin.._selectionRectEnd];
                    AdjustForSelection();
                    ResetSelection();
                }
            }

            if (Input.KeyPressed(Keys.Z))
            {
                _undoIndex--;
                if (_undoIndex >= 0)
                {
                    UndoState state = _undoStates[_undoIndex];
                    Text = state.Text;
                    _cursorPos = state.CursorPos;
                    _selectionRectBegin = state.BeginSelectionRect;
                    _selectionRectEnd = state.EndSelectionRect;
                }
                else
                    _undoIndex = 0;
            }

            if (Input.KeyPressed(Keys.Y))
            {
                _undoIndex++;
                if (_undoIndex < _undoStates.Count)
                {
                    UndoState state = _undoStates[_undoIndex];
                    Text = state.Text;
                    _cursorPos = state.CursorPos;
                    _selectionRectBegin = state.BeginSelectionRect;
                    _selectionRectEnd = state.EndSelectionRect;
                }
                else
                    _undoIndex = _undoStates.Count;
            }
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

        _selectionRectBegin = CubicMath.Clamp(_selectionRectBegin, 0, Text.Length);
        _selectionRectEnd = CubicMath.Clamp(_selectionRectEnd, 0, Text.Length);

        _textInserted = false;
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
        graphics.Scissor = rect;
        graphics.SpriteRenderer.Begin();
        
        if (!string.IsNullOrEmpty(Placeholder) && Text.Length == 0)
        {
            Theme.Font.Draw(graphics.SpriteRenderer, textSize, Placeholder,
                new Vector2(rect.X + Padding, rect.Y + rect.Height / 2), Theme.AccentTextColor, 0,
                new Vector2(0, textSize / 2), Vector2.One, smartPlacement: false);
        }

        if (_selectionRectEnd - _selectionRectBegin > 0)
        {
            int measureTextBegin = Theme.Font.MeasureString(textSize, Text[.._selectionRectBegin], ignoreParams: true).Width;
            int measureTextEnd = Theme.Font.MeasureString(textSize, Text[_selectionRectBegin.._selectionRectEnd], ignoreParams: true).Width;
            graphics.SpriteRenderer.DrawRectangle(new Vector2(rect.X + measureTextBegin - _textOffset, rect.Y + rect.Height / 2),
                new Vector2(measureTextEnd, rect.Height - 10), Theme.SelectionColor, 0, new Vector2(0, 0.5f));
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

    private void ResetSelection()
    {
        _selectionRectBegin = 0;
        _selectionRectEnd = 0;
    }

    private bool AdjustForSelection()
    {
        if (_selectionRectEnd - _selectionRectBegin > 0)
        {
            AddUndo();
            Text = Text.Remove(_selectionRectBegin, _selectionRectEnd - _selectionRectBegin);
            if (_cursorPos == _selectionRectEnd)
                _cursorPos -= _selectionRectEnd - _selectionRectBegin;
            return true;
        }

        return false;
    }

    public void AddUndo()
    {
        if (_undoIndex < _undoStates.Count)
            _undoStates.RemoveRange(_undoIndex, _undoStates.Count - _undoIndex);
        _undoStates.Add(new UndoState(Text, _cursorPos, _selectionRectBegin, _selectionRectEnd));
        _undoIndex++;
    }
    
    public delegate void OnTextChanged(string text);

    private struct UndoState
    {
        public readonly string Text;
        public readonly int CursorPos;
        public readonly int BeginSelectionRect;
        public readonly int EndSelectionRect;

        public UndoState(string text, int cursorPos, int beginSelectionRect, int endSelectionRect)
        {
            Text = text;
            CursorPos = cursorPos;
            BeginSelectionRect = beginSelectionRect;
            EndSelectionRect = endSelectionRect;
        }
    }
}