using System.Drawing;
using System.Numerics;
using Cubic.Render;
using Cubic.Render.Lighting;

namespace Cubic.Utilities;

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

    public Skybox Skybox;

    public DirectionalLight Sun = new DirectionalLight(new Vector2(0, 75), Color.White, 0.1f, 0.7f, 1.0f);
}