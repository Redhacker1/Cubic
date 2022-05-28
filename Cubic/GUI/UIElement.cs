using System;
using System.Drawing;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public abstract class UIElement
{
    public event OnHover Hover;
    public event OnClick Click;
    
    public Anchor Anchor;
    public Rectangle Position;

    public bool CaptureMouse;
    public bool IgnoreReferenceResolution;

    protected bool Hovering;
    protected bool Clicked;

    public UIElement(Anchor anchor, Rectangle position, bool captureMouse = true, bool ignoreReferenceResolution = false)
    {
        Anchor = anchor;
        Position = position;
        CaptureMouse = captureMouse;
        IgnoreReferenceResolution = ignoreReferenceResolution;
    }

    protected internal virtual void Update(ref bool mouseCaptured)
    {
        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution);

        Hovering = false;
        
        if (rect.Contains(Input.MousePosition.ToPoint()) && !mouseCaptured)
        {
            Hovering = true;
            mouseCaptured = true;
            Hover?.Invoke();

            if (Input.MouseButtonDown(MouseButtons.Left))
                Clicked = true;
        }
        if (Clicked && !Input.MouseButtonDown(MouseButtons.Left))
        {
            Clicked = false;
            Click?.Invoke();
        }
    }

    protected internal virtual void Draw(Graphics graphics) { }

    protected internal virtual void Remove() { }

    public delegate void OnClick();

    public delegate void OnHover();
}