using System.Drawing;
using Cubic2D.Render.Text;

namespace Cubic2D.GUI;

public struct UITheme
{
    public Font Font;

    public Color BorderColor;

    public int BorderWidth;

    public Color RectColor;

    public Color HoverColor;

    public Color ClickColor;

    public Color SelectedColor;

    public Color TextColor;
    
    public UITheme()
    {
        Font = default;
        
        BorderColor = Color.White;
        BorderWidth = 1;
        RectColor = Color.Gray;
        HoverColor = Color.DarkGray;
        ClickColor = Color.Gray;
        SelectedColor = Color.DarkGray;
        TextColor = Color.White;
    }
}