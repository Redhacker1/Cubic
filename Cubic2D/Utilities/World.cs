using System.Drawing;
using System.Numerics;
using Cubic2D.Render;

namespace Cubic2D.Utilities;

public class World
{
    internal Vector4 ClearColorInternal = new Vector4(0, 0, 0, 1);

    public Color ClearColor
    {
        set => ClearColorInternal = value.Normalize();
    }

    public TextureSample SampleType = TextureSample.Nearest;
}