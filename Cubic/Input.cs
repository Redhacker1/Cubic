using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Cubic.Utilities;
using Cubic.Windowing;
using static Cubic.Windowing.GameWindow;
using Silk.NET.GLFW;

namespace Cubic;

public static unsafe class Input
{
    #region Keyboard and mouse

    public static event OnTextInput TextInput;
    
    private static readonly HashSet<KeyState> _keyStates = new HashSet<KeyState>();
    private static readonly HashSet<MouseState> _mouseStates = new HashSet<MouseState>();

    private static readonly HashSet<Keys> _keysHeld = new HashSet<Keys>();
    private static readonly HashSet<Keys> _frameKeys = new HashSet<Keys>();
    private static readonly HashSet<Keys> _repeatedKeys = new HashSet<Keys>();

    private static readonly HashSet<MouseButtons> _buttonsHeld = new HashSet<MouseButtons>();
    private static readonly HashSet<MouseButtons> _frameButtons = new HashSet<MouseButtons>();

    private static Cursor* _currentCursor;
    private static bool _setCursor;

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

    public static void SetMouseCursor(Bitmap mouse)
    {
        fixed (byte* p = mouse.Data)
        {
            Image image = new Image()
            {
                Width = mouse.Size.Width,
                Height = mouse.Size.Height,
                Pixels = p
            };
            _currentCursor = GLFW.CreateCursor(&image, 0, 0);
            _setCursor = true;
        }
    }
    
    #endregion

    #region Controller

    private static readonly bool[] _connectedControllers = new bool[16];
    private static readonly GamepadState[] _gamepadStates = new GamepadState[16];
    private static GamepadState[] _prevStates = new GamepadState[16];
    
    /// <summary>
    /// Set the deadzone of the controller thumbsticks. If the thumbstick axis is below the given value, it will just
    /// return 0 for that axis. This is useful for preventing drift. (Default: (0.1, 0.1))
    /// </summary>
    public static Vector2 ControllerDeadzone { get; set; } = new Vector2(0.1f, 0.1f);

    /// <summary>
    /// Gets the total number of usable controllers connected to the system.
    /// </summary>
    public static int NumControllersConnected => _connectedControllers.Count(connected => connected);

    /// <summary>
    /// Returns true if the controller index is connected to the system.
    /// </summary>
    /// <param name="index">The controller index to check.</param>
    /// <returns>True, if the controller is connected.</returns>
    public static bool ControllerConnected(int index = 0) => _connectedControllers[index];

    /// <summary>
    /// Returns true if the given button on the given controller is held down.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <param name="index">The controller index.</param>
    /// <returns>True, if the given button is held down.</returns>
    /// <remarks>If the controller at the given index is not connected, this will just return false.</remarks>
    public static unsafe bool ControllerButtonDown(ControllerButton button, int index = 0)
    {
        return _gamepadStates[index].Buttons[(int) button] == 1;
    }

    /// <summary>
    /// Returns true if the given button on the given controller was pressed on this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <param name="index">The controller index.</param>
    /// <returns>True, if the given button was pressed on this frame.</returns>
    /// <remarks>If the controller at the given index is not connected, this will just return false.</remarks>
    public static unsafe bool ControllerButtonPressed(ControllerButton button, int index = 0)
    {
        return _gamepadStates[index].Buttons[(int) button] == 1 &&
               _prevStates[index].Buttons[(int) button] != 1;
    }

    /// <summary>
    /// Returns true if the given trigger is pressed more than <paramref name="triggerAmount"/> on this frame.
    /// </summary>
    /// <param name="trigger">The trigger to check.</param>
    /// <param name="index">The controller index.</param>
    /// <param name="triggerAmount">The amount the trigger should have to be pressed in order to return true. Should be a value between 0 and 1.</param>
    /// <returns>True, if the given trigger is pressed more than <paramref name="triggerAmount"/> on this frame.</returns>
    /// <remarks>If the controller at the given index is not connected, this will just return false.</remarks>
    public static unsafe bool ControllerAxisPressed(ControllerTrigger trigger, int index = 0,
        float triggerAmount = 0.5f)
    {
        float value = _gamepadStates[index].Axes[(int) trigger + 4];
        value = (value + 1) / 2f;
        float pValue = _prevStates[index].Axes[(int) trigger + 4];
        pValue = (pValue + 1) / 2f;

        return value >= triggerAmount && pValue < triggerAmount;
    }
    
    /// <summary>
    /// Returns true if the given stick is pressed, in the given direction, more than <paramref name="triggerAmount"/> on this frame.
    /// </summary>
    /// <param name="stick">The stick to check.</param>
    /// <param name="axis">The axis of the given thumb stick to check.</param>
    /// <param name="index">The controller index.</param>
    /// <param name="triggerAmount">The amount the stick should have to be pressed in order to return true. Should be a value between 0 and 1.</param>
    /// <returns>True, if the given stick is pressed more than <paramref name="triggerAmount"/> on this frame.</returns>
    /// <remarks>If the controller at the given index is not connected, this will just return false.</remarks>
    public static unsafe bool ControllerAxisPressed(ThumbStick stick, StickAxis axis, int index = 0,
        float triggerAmount = 0.5f)
    {
        int invert = ((int) axis % 2) == 0 ? -1 : 1;
        float value = invert * _gamepadStates[index].Axes[(int) stick * 2 + (int) axis / 2];
        float pValue = invert * _prevStates[index].Axes[(int) stick * 2 + (int) axis / 2];

        return value >= triggerAmount && pValue < triggerAmount;
    }

    /// <summary>
    /// Returns true if any of the given buttons on the given controller are held down.
    /// </summary>
    /// <param name="buttons">The buttons to check.</param>
    /// <param name="index">The controller index.</param>
    /// <returns>True, if any of the given buttons are held down.</returns>
    /// <remarks>If the controller at the given index is not connected, this will just return false.</remarks>
    public static bool ControllerButtonsDown(int index = 0, params ControllerButton[] buttons)
    {
        return buttons.Any(button => ControllerButtonDown(button, index));
    }

    /// <summary>
    /// Returns true if any of the given buttons on the given controller were pressed in this frame.
    /// </summary>
    /// <param name="buttons">The buttons to check.</param>
    /// <param name="index">The controller index.</param>
    /// <returns>True, if any of the given buttons were pressed in this frame.</returns>
    /// <remarks>If the controller at the given index is not connected, this will just return false.</remarks>
    public static bool ControllerButtonsPressed(int index = 0, params ControllerButton[] buttons)
    {
        return buttons.Any(button => ControllerButtonPressed(button, index));
    }

    /// <summary>
    /// Get the normalized <see cref="Vector2"/> axis for the given thumb stick. (Stick fully right = (1, 0). Stick fully up = (0, 1))
    /// </summary>
    /// <param name="stick">The stick to get the axis of.</param>
    /// <param name="index">The controller index.</param>
    /// <returns>The axis for the given thumb stick.</returns>
    /// <remarks>If the controller at the given index is not connected, this will just return <see cref="Vector2.Zero"/>.</remarks>
    public static unsafe Vector2 GetControllerAxis(ThumbStick stick, int index = 0)
    {
        float x = _gamepadStates[index].Axes[(int) stick * 2];
        float y = -_gamepadStates[index].Axes[(int) stick * 2 + 1];

        x = x <= ControllerDeadzone.X && x >= -ControllerDeadzone.X ? 0 : x;
        y = y <= ControllerDeadzone.Y && y >= -ControllerDeadzone.Y ? 0 : y;

        return new Vector2(x, y);
    }

    /// <summary>
    /// Get the normalized axis for the given trigger. (Fully depressed = 1, fully released = 0).
    /// </summary>
    /// <param name="trigger">The trigger to get the axis of.</param>
    /// <param name="index">The controller index.</param>
    /// <returns>The axis for the given trigger.</returns>
    /// <remarks>If the controller at the given index is not connected, this will just return 0.</remarks>
    public static unsafe float GetControllerAxis(ControllerTrigger trigger, int index = 0)
    {
        float value = _gamepadStates[index].Axes[(int) trigger + 4];
        value = (value + 1) / 2f;
        return value;
    }

    #endregion

    internal static unsafe void Start(GameWindow window)
    {
        GLFW.GetCursorPos(window.Handle, out double x, out double y);
        MousePosition = new Vector2((float) x, (float) y);
        Assembly assembly = Assembly.GetCallingAssembly();
        const string name = "Cubic.gamecontrollerdb.txt";
        using Stream stream = assembly.GetManifestResourceStream(name);
        using StreamReader reader = new StreamReader(stream);
        GLFW.UpdateGamepadMappings(reader.ReadToEnd());
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

        Array.Copy(_gamepadStates, _prevStates, _gamepadStates.Length);
        Array.Clear(_gamepadStates);
        Array.Clear(_connectedControllers);
        int totalGamepads = 0;
        for (int i = 0; i < 16; i++)
        {
            if (GLFW.GetGamepadState(i, out GamepadState state))
            {
                _connectedControllers[totalGamepads] = true;
                _gamepadStates[totalGamepads++] = state;
            }
        }

        if (_setCursor)
        {
            _setCursor = false;
            GLFW.SetCursor(window.Handle, _currentCursor);
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

    internal static unsafe void ScrollCallback(WindowHandle* windowHandle, double offsetx, double offsety)
    {
        ScrollWheelDelta += new Vector2((float) offsetx, (float) offsety);
    }

    internal static unsafe void CharCallback(WindowHandle* windowHandle, uint codepoint)
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

public enum ControllerButton
{
    A,
    B,
    X,
    Y,
    LeftBumper,
    RightBumper,
    Select,
    Start,
    Home,
    LeftThumbstick,
    RightThumbstick,
    DpadUp,
    DpadRight,
    DpadDown,
    DpadLeft
}

public enum ThumbStick
{
    LeftStick,
    RightStick,
}

public enum StickAxis
{
    Left,
    Right,
    Up,
    Down
}

public enum ControllerTrigger
{
    LeftTrigger,
    RightTrigger
}