using System.Drawing;
using System.Numerics;
using Cubic2D.Render;

namespace Cubic2D.Utilities;

public class World
{
    private Color _clearColor;
    internal Vector4 ClearColorInternal = new Vector4(0, 0, 0, 1);

    public Color ClearColor
    {
        get => _clearColor;
        set
        {
            _clearColor = value;
            ClearColorInternal = value.Normalize();
        }
    }

    public TextureSample SampleType = TextureSample.Nearest;
}