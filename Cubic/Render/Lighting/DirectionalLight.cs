using System.Drawing;
using System.Numerics;

namespace Cubic.Render.Lighting;

public struct DirectionalLight
{
    public Vector2 Direction;
    public Color Color;
    public float AmbientMultiplier;
    public float DiffuseMultiplier;
    public float SpecularMultiplier;

    public DirectionalLight(Vector2 direction, Color color, float ambientMultiplier, float diffuseMultiplier, float specularMultiplier)
    {
        Direction = direction;
        Color = color;
        AmbientMultiplier = ambientMultiplier;
        DiffuseMultiplier = diffuseMultiplier;
        SpecularMultiplier = specularMultiplier;
    }
}