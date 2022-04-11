using System.Drawing;
using Cubic.Render;

namespace Cubic.Render;

public class Material
{
    public Texture2D Albedo;
    public Texture2D Specular;
    public Color Color;
    public int Shininess;

    public Material(Texture2D albedo, Texture2D specular, Color color, int shininess)
    {
        Albedo = albedo;
        Specular = specular;
        Color = color;
        Shininess = shininess;
    }

    public Material(Texture2D albedo) : this(albedo, albedo, Color.White, 1) { }
}