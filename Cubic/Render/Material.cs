using System.Drawing;
using Cubic.Render;

namespace Cubic.Render;

public class Material
{
    public Texture Albedo;
    public Texture Specular;
    public Color Color;
    public int Shininess;

    public Material(Texture albedo, Texture specular, Color color, int shininess)
    {
        Albedo = albedo;
        Specular = specular;
        Color = color;
        Shininess = shininess;
    }

    public Material(Texture albedo) : this(albedo, albedo, Color.White, 1) { }
}