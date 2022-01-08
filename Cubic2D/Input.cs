using System.Collections.Generic;
using System.Linq;
using Veldrid;

namespace Cubic2D;

public static class Input
{
    private static HashSet<Keys> _keysHeld = new HashSet<Keys>();
    private static HashSet<Keys> _frameKeys = new HashSet<Keys>();

    public static Keys[] KeysHeld => _keysHeld.ToArray();

    public static bool KeyDown(Keys key) => _keysHeld.Contains(key);

    public static bool KeyPressed(Keys key) => _frameKeys.Contains(key);
    
    internal static void Update(InputSnapshot snapshot)
    {
        _frameKeys.Clear();
        
        foreach (KeyEvent e in snapshot.KeyEvents)
        {
            if (e.Down)
            {
                if (_keysHeld.Add((Keys) e.Key))
                    _frameKeys.Add((Keys) e.Key);
            }
            else
            {
                _keysHeld.Remove((Keys) e.Key);
                _frameKeys.Remove((Keys) e.Key);
            }
        }
    }
}

public enum Keys
{
    Unknown,
    LeftShift,
    RightShift,
    LeftControl,
    RightControl,
    LeftAlt,
    RightAlt,
    LeftSuper,
    RightSuper,
    Menu,
    F1,
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
    F26,
    F27,
    F28,
    F29,
    F30,
    F31,
    F32,
    F33,
    F34,
    F35,
    Up,
    Down,
    Left,
    Right,
    Enter,
    Escape,
    Space,
    Tab,
    Backspace,
    Insert,
    Delete,
    PageUp,
    PageDown,
    Home,
    End,
    CapsLock,
    ScrollLock,
    PrintScreen,
    Pause,
    NumLock,
    Clear,
    Sleep,
    Keypad0,
    Keypad1,
    Keypad2,
    Keypad3,
    Keypad4,
    Keypad5,
    Keypad6,
    Keypad7,
    Keypad8,
    Keypad9,
    KeypadDivide,
    KeypadMultiply,
    KeypadSubtract,
    KeypadAdd,
    KeypadDecimal,
    KeypadEnter,
    A,
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
    Tilde,
    Minus,
    Plus,
    LeftBracket,
    RightBracket,
    Semicolon,
    Quote,
    Comma,
    Period,
    Slash,
    Backslash,
    NonUsBackslash
}