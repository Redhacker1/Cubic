﻿using System;
using System.Drawing;
using System.Numerics;
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
    public bool Visible;

    protected bool Hovering;
    protected bool Clicked;
    protected Point ClickPoint;

    public Vector2 Offset;

    public Rectangle? Viewport;

    public UITheme Theme;

    public UIElement(Anchor anchor, Rectangle position, bool captureMouse = true, bool ignoreReferenceResolution = false, Point? index = null)
    {
        Anchor = anchor;
        Position = position;
        CaptureMouse = captureMouse;
        IgnoreReferenceResolution = ignoreReferenceResolution;
        Viewport = null;
        Visible = true;
        Offset = Vector2.Zero;
        Theme = UI.Theme;
    }

    protected internal virtual void Update(ref bool mouseCaptured)
    {
        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);

        Hovering = false;
        
        if (rect.Contains(Input.MousePosition.ToPoint()) && !mouseCaptured)
        {
            Hovering = true;
            mouseCaptured = true;
            Hover?.Invoke();

            if (Input.MouseButtonDown(MouseButtons.Left))
            {
                Clicked = true;
                ClickPoint = new Point((int) Input.MousePosition.X - rect.X, (int) Input.MousePosition.Y - rect.Y);
            }
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