using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public class View : UIElement
{
    protected Dictionary<string, UIElement> _elements;
    private Vector2 _offset;
    private Vector2 _maxOffset;

    public View(Anchor anchor, Rectangle position, bool captureMouse = true, bool ignoreReferenceResolution = false,
        Point? index = null) : base(anchor, position, captureMouse, ignoreReferenceResolution, index)
    {
        _elements = new Dictionary<string, UIElement>();
        _maxOffset = Vector2.Zero;
    }
    
    public View AddElement(string name, UIElement element)
    {
        Rectangle winRect = Position;
        UI.CalculatePos(Anchor, ref winRect, false, Offset, Viewport);
        element.Viewport = winRect;

        winRect = Position;
        UI.CalculatePos(Anchor.TopLeft, ref winRect, false, Offset);
        Rectangle objRect = element.Position;
        UI.CalculatePos(element.Anchor, ref objRect, IgnoreReferenceResolution, Offset,
            Viewport.HasValue
                ? new Rectangle(new Point(Viewport.Value.X + winRect.X, Viewport.Value.Y + winRect.Y),
                    Viewport.Value.Size + winRect.Size)
                : winRect);

        if (objRect.Bottom >= winRect.Height)
            _maxOffset.Y = objRect.Bottom - winRect.Height;

        _elements.Add(name, element);
        return this;
    }
    
    public T GetElement<T>(string name) where T : UIElement
    {
        return (T) _elements[name];
    }
    
    protected internal override void Update(ref bool mouseCaptured)
    {
        _offset.Y += Input.ScrollWheelDelta.Y * 50;
        _offset.Y = CubicMath.Clamp(_offset.Y, -_maxOffset.Y, 0);

        bool mc = false;
        foreach ((string name, UIElement element) in _elements)
        {
            element.Offset = _offset;
            element.Update(ref mc);
        }

        base.Update(ref mouseCaptured);
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);
        
        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);
        
        graphics.SpriteRenderer.DrawRectangle(rect.Location.ToVector2(), rect.Size.ToVector2(), Color.White, 0, Vector2.Zero);

        graphics.SpriteRenderer.End();
        Rectangle scissor = graphics.Scissor;
        graphics.Scissor = rect;
        graphics.SpriteRenderer.Begin();

        foreach ((string name, UIElement element) in _elements)
            element.Draw(graphics);
        
        graphics.SpriteRenderer.End();
        graphics.Scissor = scissor;
        graphics.SpriteRenderer.Begin();
    }
}