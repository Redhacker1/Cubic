using System.Drawing;
using Cubic.Render.Text;

namespace Cubic.GUI;

public struct UITheme
{
    public Font Font;

    public Color BorderColor;

    public int BorderWidth;

    public Color RectColor;

    public Color HoverColor;

    public Color ClickColor;

    public Color TextColor;

    public int CheckBoxPadding;

    public Color CheckedColor;

    public Color WindowColor;

    public Color AccentTextColor;
    
    public UITheme()
    {
        Font = UI.DefaultFont;
        
        BorderColor = Color.Black;
        BorderWidth = 1;
        RectColor = Color.GhostWhite;
        HoverColor = Color.DarkGray;
        ClickColor = Color.LightGray;
        TextColor = Color.Black;
        CheckBoxPadding = 5;
        CheckedColor = Color.DimGray;
        WindowColor = Color.White;
        AccentTextColor = Color.DimGray;
    }
}