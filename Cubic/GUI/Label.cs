using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Utilities;

namespace Cubic.GUI;

public class Label : UIElement
{
    public string Text;
    public uint TextSize;
    
    public Label(Anchor anchor, Point position, string text, uint size = 24, bool captureMouse = true,
        bool ignoreReferenceResolution = false, Point? index = null) : base(anchor, new Rectangle(position, Size.Empty),
        captureMouse, ignoreReferenceResolution, index)
    {
        Text = text;
        TextSize = size;
    }

    protected internal override void Draw(Graphics graphics)
    {
        base.Draw(graphics);

        Rectangle rect = Position;
        uint textSize = (uint) (TextSize * UI.GetReferenceMultiplier());
        Position.Size = Theme.Font.MeasureString(TextSize, Text);
        UI.CalculatePos(Anchor, ref rect, IgnoreReferenceResolution, Offset, Viewport);

        Theme.Font.Draw(graphics.SpriteRenderer, textSize, Text, rect.Location.ToVector2(), Theme.TextColor, 0,
            Vector2.Zero, Vector2.One);
    }
}