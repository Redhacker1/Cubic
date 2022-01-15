using System.Drawing;
using Veldrid;

namespace Cubic2D.Utilities;

public class World
{
    internal RgbaFloat ClearColorInternal = RgbaFloat.Black;

    public Color ClearColor
    {
        set => ClearColorInternal = new RgbaFloat(value.Normalize());
    }
}