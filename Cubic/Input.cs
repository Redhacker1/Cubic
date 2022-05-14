using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cubic.Windowing;
using static Cubic.Windowing.GameWindow;
using Silk.NET.GLFW;

namespace Cubic;

public static class Input
{
    public static event OnTextInput TextInput;
    
    private static readonly HashSet<KeyState> _keyStates = new HashSet<KeyState>();
    private static readonly HashSet<MouseState> _mouseStates = new HashSet<MouseState>();

    private static readonly HashSet<Keys> _keysHeld = new HashSet<Keys>();
    private static readonly HashSet<Keys> _frameKeys = new HashSet<Keys>();
    private static readonly HashSet<Keys> _repeatedKeys = new HashSet<Keys>();

    private static readonly HashSet<MouseButtons> _buttonsHeld = new HashSet<MouseButtons>();
    private static readonly HashSet<MouseButtons> _frameButtons = new HashSet<MouseButtons>();

    private static MouseMode _mouseMode;
    private static bool _cursorStateChanged;

    /// <summary>
    /// Get an array of all keyboard keys currently held down.
    /// </summary>
    public static Keys[] KeysHeld => _keysHeld.ToArray();

    /// <summary>
    /// Get an array of all mouse buttons currently held down.
    /// </summary>
    public static MouseButtons[] MouseButtonsHeld => _buttonsHeld.ToArray();

    /// <summary>
    /// Check if the given key is held down.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is held down.</returns>
    public static bool KeyDown(Keys key) => _keysHeld.Contains(key);

    /// <summary>
    /// Check if the given key was pressed <b>in this current frame.</b>
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was pressed in this current frame.</returns>
    public static bool KeyPressed(Keys key) => _frameKeys.Contains(key);

    /// <summary>
    /// Check if the given key is <b>not</b> held down.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is <b>not</b> held down.</returns>
    public static bool KeyReleased(Keys key) => !_keysHeld.Contains(key);

    /// <summary>
    /// Check if the given key is held and repeating. This is useful for dealing with a repeating action that does not
    /// occur once per frame, such as navigation in a text box.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is repeating.</returns>
    public static bool KeyRepeat(Keys key) => _repeatedKeys.Contains(key);

    /// <summary>
    /// Check if the given key is pressed or is repeating. This is useful for dealing with a repeating action that does
    /// not occur once per frame, such as navigation in a text box.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the given key is pressed or repeating.</returns>
    /// <remarks>This method acts like <see cref="KeyPressed"/> and <see cref="KeyRepeat"/> combined.</remarks>
    public static bool KeyPressedOrRepeating(Keys key) => _frameKeys.Contains(key) || _repeatedKeys.Contains(key);

    /// <summary>
    /// Check if any of the given keys are held down.
    /// </summary>
    /// <param name="keys">The keys to check.</param>
    /// <returns>True if any of the given keys are held down.</returns>
    public static bool KeysDown(params Keys[] keys)
    {
        foreach (Keys key in keys)
        {
            if (_keysHeld.Contains(key))
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// Check if any of the given keys were pressed <b>in this current frame.</b>
    /// </summary>
    /// <param name="keys">The keys to check.</param>
    /// <returns>True if any of the given keys were pressed in this current frame.</returns>
    public static bool KeysPressed(params Keys[] keys)
    {
        foreach (Keys key in keys)
        {
            if (_frameKeys.Contains(key))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if the given mouse button is held down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the mouse button is held down.</returns>
    public static bool MouseButtonDown(MouseButtons button) => _buttonsHeld.Contains(button);

    /// <summary>
    /// Check if the given mouse button was pressed <b>in this current frame.</b>
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the mouse button was pressed in this current frame.</returns>
    public static bool MouseButtonPressed(MouseButtons button) => _frameButtons.Contains(button);

    /// <summary>
    /// Check if the given mouse button is <b>not</b> held down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the mouse button is <b>not</b> held down.</returns>
    public static bool MouseButtonReleased(MouseButtons button) => !_buttonsHeld.Contains(button);
    
    /// <summary>
    /// Check if any of the given mouse buttons are held down.
    /// </summary>
    /// <param name="buttons">The mouse buttons to check.</param>
    /// <returns>True if any of the given mouse buttons are held down.</returns>
    public static bool MouseButtonsDown(params MouseButtons[] buttons)
    {
        foreach (MouseButtons button in buttons)
        {
            if (_buttonsHeld.Contains(button))
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// Check if any of the given mouse buttons were pressed <b>in this current frame.</b>
    /// </summary>
    /// <param name="buttons">The mouse buttons to check.</param>
    /// <returns>True if any of the given mouse buttons were pressed in this current frame.</returns>
    public static bool MouseButtonsPressed(params MouseButtons[] buttons)
    {
        foreach (MouseButtons button in buttons)
        {
            if (_frameButtons.Contains(button))
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// The current position of the mouse cursor on screen, relative to the top-left of the window.
    /// </summary>
    public static Vector2 MousePosition { get; private set; }
    
    /// <summary>
    /// The total number of pixels the mouse cursor has moved since the last frame.
    /// </summary>
    public static Vector2 MousePositionDelta { get; private set; }
    
    /// <summary>
    /// The change in scroll since the last frame.
    /// </summary>
    public static Vector2 ScrollWheelDelta { get; private set; }

    public static MouseMode MouseMode
    {
        get => _mouseMode;
        set
        {
            _mouseMode = value;
            _cursorStateChanged = true;
        }
    }

    /// <summary>
    /// Transform the mouse position by the given matrix.
    /// </summary>
    /// <param name="transform">The transform matrix.</param>
    public static void TransformMousePosition(Matrix4x4 transform)
    {
        Matrix4x4.Invert(transform, out Matrix4x4 invTransform);
        MousePosition = Vector2.Transform(MousePosition, invTransform);
    }

    internal static unsafe void Start(GameWindow window)
    {
        GLFW.GetCursorPos(window.Handle, out double x, out double y);
        MousePosition = new Vector2((float) x, (float) y);
    }

    internal static unsafe void Update(GameWindow window)
    {
        _keyStates.Clear();
        _mouseStates.Clear();
        _frameKeys.Clear();
        _frameButtons.Clear();
        _repeatedKeys.Clear();
        ScrollWheelDelta = Vector2.Zero;

        GLFW.PollEvents();

        foreach (KeyState state in _keyStates)
        {
            if (state.Pressed)
            {
                if (_keysHeld.Add(state.Key))
                    _frameKeys.Add(state.Key);
            }
            else
            {
                _keysHeld.Remove(state.Key);
                _frameKeys.Remove(state.Key);
            }
        }

        foreach (MouseState state in _mouseStates)
        {
            if (state.Pressed)
            {
                if (_buttonsHeld.Add(state.Button))
                    _frameButtons.Add(state.Button);
            }
            else
            {
                _buttonsHeld.Remove(state.Button);
                _frameButtons.Remove(state.Button);
            }
        }

        GLFW.GetCursorPos(window.Handle, out double x, out double y);
        Vector2 mPos = new Vector2((float) x, (float) y);
        MousePositionDelta = mPos - MousePosition;
        MousePosition = mPos;

        if (_cursorStateChanged)
        {
            _cursorStateChanged = false;
            CursorModeValue val = _mouseMode switch
            {
                MouseMode.Visible => CursorModeValue.CursorNormal,
                MouseMode.Hidden => CursorModeValue.CursorHidden,
                MouseMode.Locked => CursorModeValue.CursorDisabled,
                _ => throw new ArgumentOutOfRangeException()
            };
            GLFW.SetInputMode(window.Handle, CursorStateAttribute.Cursor, val);
        }
    }

    internal static unsafe void KeyCallback(WindowHandle* windowHandle, Silk.NET.GLFW.Keys keys,
        int scanCode, InputAction action, KeyModifiers mods)
    {
        if (action == InputAction.Repeat)
            _repeatedKeys.Add((Keys) keys);
        else
            _keyStates.Add(new KeyState((Keys) keys, action == InputAction.Press));
    }

    internal static unsafe void MouseCallback(WindowHandle* windowHandle, MouseButton button, InputAction action, KeyModifiers mods)
    {
        if (action != InputAction.Repeat)
            _mouseStates.Add(new MouseState((MouseButtons) button, action == InputAction.Press));
    }

    private struct KeyState
    {
        public Keys Key;
        public bool Pressed;

        public KeyState(Keys key, bool pressed)
        {
            Key = key;
            Pressed = pressed;
        }
    }
    
    private struct MouseState
    {
        public MouseButtons Button;
        public bool Pressed;

        public MouseState(MouseButtons button, bool pressed)
        {
            Button = button;
            Pressed = pressed;
        }
    }

    public static unsafe void ScrollCallback(WindowHandle* windowHandle, double offsetx, double offsety)
    {
        ScrollWheelDelta += new Vector2((float) offsetx, (float) offsety);
    }

    public static unsafe void CharCallback(WindowHandle* windowHandle, uint codepoint)
    {
        TextInput?.Invoke((char) codepoint);
    }

    public delegate void OnTextInput(char character);
}

public enum Keys
{
    Unknown = -1,
    Space = 32,
    Apostrophe = 39,
    Comma = 44,
    Minus,
    Period,
    Slash,
    Num0,
    Num1,
    Num2,
    Num3,
    Num4,
    Num5,
    Num6,
    Num7,
    Num8,
    Num9,
    Semicolon = 59,
    Equals = 61,
    A = 65,
    B,
    C,
    D,
    E,
    F,
    G,
    H,
    I,
    J,
    K,
    L,
    M,
    N,
    O,
    P,
    Q,
    R,
    S,
    T,
    U,
    V,
    W,
    X,
    Y,
    Z,
    LeftBracket,
    Backslash,
    RightBracket,
    GraveAccent = 96,
    Backtick = 96,
    World1 = 161,
    World2 = 162,
    Escape = 256,
    Enter,
    Tab,
    Backspace,
    Insert,
    Delete,
    Right,
    Left,
    Down,
    Up,
    PageUp,
    PageDown,
    Home,
    End,
    CapsLock = 280,
    ScrollLock,
    NumLock,
    PrintScreen,
    Pause,
    F1 = 290,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12,
    F13,
    F14,
    F15,
    F16,
    F17,
    F18,
    F19,
    F20,
    F21,
    F22,
    F23,
    F24,
    F25,
    Keypad0 = 320,
    Keypad1,
    Keypad2,
    Keypad3,
    Keypad4,
    Keypad5,
    Keypad6,
    Keypad7,
    Keypad8,
    Keypad9,
    KeypadDecimal = 330,
    KeypadPeriod = 330,
    KeypadDivide,
    KeypadMultiply,
    KeypadSubtract,
    KeypadAdd,
    KeypadEnter,
    KeypadEquals,
    LeftShift = 340,
    LeftControl,
    LeftAlt,
    LeftSuper,
    RightShift,
    RightControl,
    RightAlt,
    RightSuper,
    Menu
}

public enum MouseButtons
{
    Left = 0,
    Right = 1,
    Middle = 2,
    Button1 = 0,
    Button2 = 1,
    Button3 = 2,
    Button4,
    Button5,
    Button6,
    Button7,
    Button8
}

public enum MouseMode
{
    Visible,
    Hidden,
    Locked
}