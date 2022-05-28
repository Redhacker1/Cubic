using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public static partial class UI
{
    private static (int, Rectangle)[] _lastElementPositions;
    private static List<(int, Rectangle)> _elementPositions;
    private static List<(Rectangle, Color, Texture2D, int)> _rectangles;
    private static List<(string text, uint size, Vector2 pos, Color color, bool centerOrigin, bool ignoreParams, int id)> _texts;
    private static List<char> _charBuffer;
    private static int _hoveringID;
    private static int _currentID;
    private static int _clickedID;

    private static bool _mouseButtonHeld;
    private static bool _clicked;

    private static Size _framebufferSize;

    public static UITheme Theme;
    public static Size ReferenceResolution;

    private static Dictionary<string, UIElement> _elements;
    private static List<UIElement> _reversedElements;

    internal static void Initialize(Size viewportSize)
    {
        _framebufferSize = viewportSize;
        _rectangles = new List<(Rectangle, Color, Texture2D, int)>();
        _texts = new List<(string, uint, Vector2, Color, bool, bool, int)>();
        _elements = new Dictionary<string, UIElement>();
        _reversedElements = new List<UIElement>();
        _elementPositions = new List<(int, Rectangle)>();
        Theme = new UITheme();
        _charBuffer = new List<char>();
        _textCursorPos = Point.Empty;
        ReferenceResolution = Size.Empty;
        Input.TextInput += TextEntered;
    }

    private static void TextEntered(char character)
    {
        _charBuffer.Add(character);
    }

    private static bool MouseHovering(Rectangle pos)
    {
        Vector2 mPos = Input.MousePosition;
        return pos.Contains(new Point((int) mPos.X, (int) mPos.Y)) && _currentID == _hoveringID;
    }

    private static bool ElementClicked(Rectangle pos)
    {
        Vector2 mPos = Input.MousePosition;
        if (!_clicked || !pos.Contains(new Point((int) mPos.X, (int) mPos.Y)) || _currentID != _hoveringID)
            return false;
        _clickedID = _currentID;
        return true;
    }

    private static void Add(Rectangle pos, bool captureMouse = true)
    {
        _currentID++;
        if (captureMouse)
            _elementPositions.Add((_currentID, pos));
    }

    public static float GetReferenceMultiplier()
    {
        if (ReferenceResolution == Size.Empty)
            return 1;
        float refSize = _framebufferSize.Width > _framebufferSize.Height
            ? _framebufferSize.Height
            : _framebufferSize.Width;
        return refSize / (_framebufferSize.Width > _framebufferSize.Height
            ? ReferenceResolution.Height
            : ReferenceResolution.Width);
    }

    internal static void CalculatePos(Anchor anchor, ref Rectangle rect, bool ignoreReference)
    {
        Vector2 origin;
        float scale = ignoreReference ? 1 : GetReferenceMultiplier();
        
        switch (anchor)
        {
            case Anchor.TopLeft:
                rect.X = (int) (rect.X * scale);
                rect.Y = (int) (rect.Y * scale);
                origin = Vector2.Zero;
                break;
            case Anchor.TopCenter:
                rect.X = (int) (_framebufferSize.Width / 2f + rect.X * scale);
                rect.Y = (int) (rect.Y * scale);
                origin = new Vector2(rect.Size.Width / 2, 0);
                break;
            case Anchor.TopRight:
                rect.X = (int) (_framebufferSize.Width + rect.X * scale);
                rect.Y = (int) (rect.Y * scale);
                origin = new Vector2(rect.Size.Width, 0);
                break;
            case Anchor.CenterLeft:
                rect.X = (int) (rect.X * scale);
                rect.Y = (int) (_framebufferSize.Height / 2f + rect.Y * scale);
                origin = new Vector2(0, rect.Size.Height / 2);
                break;
            case Anchor.Center:
                rect.X = (int) (_framebufferSize.Width / 2f + rect.X * scale);
                rect.Y = (int) (_framebufferSize.Height / 2f + rect.Y * scale);
                origin = rect.Size.ToVector2() / 2;
                break;
            case Anchor.CenterRight:
                rect.X = (int) (_framebufferSize.Width + rect.X * scale);
                rect.Y = (int) (_framebufferSize.Height / 2f + rect.Y * scale);
                origin = new Vector2(rect.Size.Width, rect.Size.Height / 2);
                break;
            case Anchor.BottomLeft:
                rect.X = (int) (rect.X * scale);
                rect.Y = (int) (_framebufferSize.Height + rect.Y * scale);
                origin = new Vector2(0, rect.Size.Height);
                break;
            case Anchor.BottomCenter:
                rect.X = (int) (_framebufferSize.Width / 2f + rect.X * scale);
                rect.Y = (int) (_framebufferSize.Height + rect.Y * scale);
                origin = new Vector2(rect.Size.Width / 2, rect.Size.Height);
                break;
            case Anchor.BottomRight:
                rect.X = (int) (_framebufferSize.Width + rect.X * scale);
                rect.Y = (int) (_framebufferSize.Height + rect.Y * scale);
                origin = rect.Size.ToVector2();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null);
        }

        rect.Width = (int) (rect.Width * scale);
        rect.Height = (int) (rect.Height * scale);

        rect.X -= (int) (origin.X * scale);
        rect.Y -= (int) (origin.Y * scale);
    }

    public static void AddElement(string name, UIElement element)
    {
        _elements.Add(name, element);
        _reversedElements.Add(element);
    }

    private static bool IsFocused()
    {
        // TODO
        return _clickedID == _currentID;
    }

    internal static void Update()
    {
        _lastElementPositions = _elementPositions.ToArray();
        _rectangles.Clear();
        _texts.Clear();
        _elementPositions.Clear();
        _hoveringID = -1;
        _currentID = -1;
        _clicked = false;

        Vector2 mousePos = Input.MousePosition;
        for (int i = _lastElementPositions.Length - 1; i >= 0; i--)
        {
            if (!_lastElementPositions[i].Item2.Contains(new Point((int) mousePos.X, (int) mousePos.Y))) continue;
            _hoveringID = _lastElementPositions[i].Item1;
            break;
        }

        if (Input.MouseButtonDown(MouseButtons.Left))
            _mouseButtonHeld = true;
        if (Input.MouseButtonReleased(MouseButtons.Left) && _mouseButtonHeld)
        {
            _clicked = true;
            _mouseButtonHeld = false;
        }

        bool mouseCaptured = false;
        for (int i = _reversedElements.Count - 1; i >= 0; i--)
        {
            _reversedElements[i].Update(ref mouseCaptured);
        }
    }

    internal static void Draw(Graphics graphics)
    {
        // Update the framebuffer size per frame to allow the anchoring system to work.
        _framebufferSize = graphics.Viewport.Size;
        
        // I'd rather not do this in draw but I have to due to the way the updating works
        _charBuffer.Clear();
        
        // UI is drawn on a different spritebatch, and always drawn on top.
        graphics.SpriteRenderer.Begin();
        
        // Explanation of what's going on below
        // ----------------------------------------
        // It first loops through all the rectangles, and draws those. Then text, and draws those too.
        // However one thing you should notice is that they are drawn separately. All rectangles are drawn before any
        // text is drawn. Okay, no big deal. "But what about if the elements overlap!" I hear you not asking.
        // Good question. You see, this presents a problem - since text is drawn after all rectangles are drawn, this
        // will actually cause any text that should be hidden by an overlapping rectangle will be drawn on top of it
        // instead. Bummer.
        // Fortunately, this is where my new depth sorting system comes in! Since all elements here are drawn in the
        // same batch session, we can use the depth sorting algorithm to determine what gets drawn where.
        // Each tuple here is assigned an "ID". Every element has a unique ID. Above, this is used to determine which
        // element the mouse is hovering over. But, we use it here to determine depth.
        // We actually use the ID of the item as its depth (although we negate it since we want each element drawing
        // in front of the last one). Now, since the sprite renderer sorts by depth, this means that it will automagically
        // sort all rectangles and text into the correct order, meaning it displays correctly! It uses a few more draw
        // calls, which is a shame, but modern GPUs can handle thousands of them, so it's really not a concern.

        foreach ((Rectangle, Color, Texture2D, int) cRect in _rectangles)
        {
            Rectangle rect = cRect.Item1;
            Color col = cRect.Item2;
            //graphics.SetScissor(_elementPositions[cRect.Item3]);
            graphics.SpriteRenderer.Draw(cRect.Item3, new Vector2(rect.X, rect.Y), null, col, 0, Vector2.Zero,
                new Vector2(rect.Width / (float) cRect.Item3.Size.Width, rect.Height / (float) cRect.Item3.Size.Height), SpriteFlipMode.None, -cRect.Item4);
        }

        foreach ((string text, uint size, Vector2 pos, Color color, bool centerOrigin, bool ignoreParams, int id) in _texts)
        {
            Vector2 origin = Vector2.Zero;
            if (centerOrigin)
                origin = Theme.Font.MeasureString(size, text).ToVector2() / 2;
            origin.X = (int) origin.X;
            origin.Y = (int) origin.Y;
            
            Theme.Font.Draw(graphics.SpriteRenderer, size, text, pos, color, 0, origin, Vector2.One, -id,
                ignoreParams: ignoreParams);
        }
        
        foreach (UIElement element in _reversedElements)
            element.Draw(graphics);

        graphics.SpriteRenderer.End();
    }

    public static void Clear()
    {
        _elements.Clear();
        _reversedElements.Clear();
    }
}