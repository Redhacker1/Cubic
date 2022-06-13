using System;
using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public abstract class UIElement
{
    /// <summary>
    /// This event is fired whenever this element is hovered.
    /// </summary>
    public event OnHover Hover;
    
    /// <summary>
    /// This event is fired whenever this element is clicked.
    /// </summary>
    public event OnClick Click;
    
    /// <summary>
    /// The anchor of this element.
    /// </summary>
    public Anchor Anchor;
    
    /// <summary>
    /// The position, relative to the anchor, of this element. For most elements, the size of the rectangle is the size
    /// of the element itself.
    /// </summary>
    public Rectangle Position;

    /// <summary>
    /// If true (default), when this element is hovered over, elements underneath this element will not be highlighted.
    /// If false, elements underneath this element will treat it like this element does not exist.
    /// </summary>
    public bool CaptureMouse;
    
    /// <summary>
    /// If true, the defined reference resolution will be ignored and this element will not scale to the window size.
    /// </summary>
    public bool IgnoreReferenceResolution;
    
    /// <summary>
    /// If false, this element will not be visible, and will act like it does not exist (i.e. mouse events will not be
    /// processed)
    /// </summary>
    public bool Visible;

    protected bool Hovering;
    protected bool Clicked;
    protected bool Pressed;
    protected Point ClickPoint;

    /// <summary>
    /// The scroll offset of the element. It's down to the element to process this. <b>Please don't modify this value.</b>
    /// </summary>
    public Vector2 Offset;

    /// <summary>
    /// The viewport of this element. Used for relative element positioning. <b>Please don't modify this value.</b>
    /// </summary>
    public Rectangle? Viewport;

    /// <summary>
    /// The individual theme for this element. By default, this will use UI.<see cref="UI.Theme"/>
    /// </summary>
    public UITheme Theme;

    /// <summary>
    /// If true, certain events (such as text entry for text boxes) will be processed.
    /// </summary>
    public bool Focused;

    /// <summary>
    /// Custom data storage for this element. Useful for buttons which have been added programmatically.
    /// Store anything here you like, as it can be retrieved later.
    /// </summary>
    public object UserStorage;

    /// <summary>
    /// Used to determine if this element can be hovered. <b>Please don't modify this value.</b>
    /// </summary>
    public bool AllowHover;

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
        AllowHover = true;
    }

    protected internal virtual void Update(ref bool mouseCaptured)
    {
        Rectangle rect = Position;
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);

        Hovering = false;
        Pressed = false;
        
        if (AllowHover && !mouseCaptured && rect.Contains(Input.MousePosition.ToPoint()))
        {
            Hovering = true;
            if (CaptureMouse)
                mouseCaptured = true;
            Hover?.Invoke();

            if (Input.MouseButtonDown(MouseButtons.Left))
            {
                Clicked = true;
                ClickPoint = new Point((int) Input.MousePosition.X - rect.X, (int) Input.MousePosition.Y - rect.Y);
                Focused = true;
            }
            else if (Clicked)
            {
                Clicked = false;
                Click?.Invoke();
                Pressed = true;
            }
        }
        else
        {
            if (Input.MouseButtonDown(MouseButtons.Left))
                Focused = false;
            
            Clicked = false;
            Pressed = false;
        }
    }

    protected internal virtual void Draw(Graphics graphics) { }

    protected internal virtual void Remove() { }

    public delegate void OnClick();

    public delegate void OnHover();
}