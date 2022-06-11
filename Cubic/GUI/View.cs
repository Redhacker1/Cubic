using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

/// <summary>
/// Represents a scrollable area for UI elements. Views will automatically enable scrolling if an element goes beyond
/// its boundaries. Views can be placed inside each other.
/// </summary>
public class View : UIElement
{
    protected Dictionary<string, UIElement> _elements;
    private Vector2 _offset;

    /// <summary>
    /// The scroll multiplier of the view. The higher the value, the faster it will scroll.
    /// To disable scrolling, set this value to 0.
    /// </summary>
    public float ScrollMultiplier;

    public View(Anchor anchor, Rectangle position, bool captureMouse = true, bool ignoreReferenceResolution = false,
        Point? index = null) : base(anchor, position, captureMouse, ignoreReferenceResolution, index)
    {
        _elements = new Dictionary<string, UIElement>();
        ScrollMultiplier = 50;
    }
    
    public View AddElement(string name, UIElement element)
    {
        _elements.Add(name, element);
        return this;
    }
    
    public T GetElement<T>(string name) where T : UIElement
    {
        return (T) _elements[name];
    }

    public UIElement[] GetAllElements() => _elements.Values.ToArray();

    protected internal override void Update(ref bool mouseCaptured)
    {
        //_offset.Y = CubicMath.Clamp(_offset.Y, -_maxOffset.Y, 0);

        int maxOffset = 0;
        bool mc = mouseCaptured;
        Rectangle winRect = Position;
        UI.CalculatePos(Anchor, ref winRect, false, Offset, Viewport);
        foreach ((string name, UIElement element) in _elements)
        {
            if (!element.Visible)
                continue;
            element.AllowHover = Hovering;
            element.Viewport = winRect;
            Rectangle pos = element.Position;
            UI.CalculatePos(element.Anchor, ref pos, true, Vector2.Zero, Position);
            if (pos.Bottom > Position.Bottom)
                maxOffset = pos.Bottom - Position.Bottom;
            element.Offset = _offset;
            element.Update(ref mc);
        }

        if (Hovering)
        {
            _offset.Y += Input.ScrollWheelDelta.Y * ScrollMultiplier;
            if (_offset.Y > 0)
                _offset.Y = 0;
        }
            
        if (_offset.Y <= -maxOffset)
            _offset.Y = -maxOffset;

        base.Update(ref mouseCaptured);
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);
        
        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);
        
        graphics.SpriteRenderer.DrawRectangle(rect.Location.ToVector2(), rect.Size.ToVector2(), Theme.WindowColor, 0, Vector2.Zero);

        graphics.SpriteRenderer.End();
        Rectangle scissor = graphics.Scissor;
        graphics.Scissor = rect;
        graphics.SpriteRenderer.Begin();

        foreach ((string name, UIElement element) in _elements)
        {
            if (!element.Visible)
                continue;
            
            element.Draw(graphics);
        }

        graphics.SpriteRenderer.End();
        graphics.Scissor = scissor;
        graphics.SpriteRenderer.Begin();
    }
}