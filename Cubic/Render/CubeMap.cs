using Cubic.Utilities;
using static Cubic.Render.Graphics;
using Silk.NET.OpenGL;

namespace Cubic.Render;

public class CubeMap : Texture
{
    public unsafe CubeMap(Bitmap top, Bitmap bottom, Bitmap front, Bitmap back, Bitmap right, Bitmap left, bool autoDispose = true) : base(autoDispose)
    {
        Handle = Gl.GenTexture();
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.TextureCubeMap, Handle);

        fixed (byte* p = right.Data)
            Gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX, 0, InternalFormat.Rgba, (uint) right.Size.Width, (uint) right.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, p);
        fixed (byte* p = left.Data)
            Gl.TexImage2D(TextureTarget.TextureCubeMapNegativeX, 0, InternalFormat.Rgba, (uint) left.Size.Width, (uint) left.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, p);
        fixed (byte* p = top.Data)
            Gl.TexImage2D(TextureTarget.TextureCubeMapPositiveY, 0, InternalFormat.Rgba, (uint) top.Size.Width, (uint) top.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, p);
        fixed (byte* p = bottom.Data)
            Gl.TexImage2D(TextureTarget.TextureCubeMapNegativeY, 0, InternalFormat.Rgba, (uint) bottom.Size.Width, (uint) bottom.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, p);
        fixed (byte* p = back.Data)
            Gl.TexImage2D(TextureTarget.TextureCubeMapNegativeZ, 0, InternalFormat.Rgba, (uint) back.Size.Width, (uint) back.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, p);
        fixed (byte* p = front.Data)
            Gl.TexImage2D(TextureTarget.TextureCubeMapPositiveZ, 0, InternalFormat.Rgba, (uint) front.Size.Width, (uint) front.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, p);

        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int) TextureMinFilter.Linear);
        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int) TextureMagFilter.Linear);
        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int) TextureWrapMode.ClampToEdge);
        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int) TextureWrapMode.ClampToEdge);
        Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int) TextureWrapMode.ClampToEdge);
        
        Gl.BindTexture(TextureTarget.TextureCubeMap, 0);
    }

    internal override void Bind(TextureUnit textureUnit = TextureUnit.Texture0)
    {
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.TextureCubeMap, Handle);
    }

    internal override void Unbind()
    {
        Gl.BindTexture(TextureTarget.TextureCubeMap, 0);
    }
}