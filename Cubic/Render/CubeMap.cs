using Cubic.Utilities;
using OpenTK.Graphics.OpenGL4;

namespace Cubic.Render;

public class CubeMap : Texture
{
    public CubeMap(Bitmap top, Bitmap bottom, Bitmap front, Bitmap back, Bitmap right, Bitmap left, bool autoDispose = true) : base(autoDispose)
    {
        Handle = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.TextureCubeMap, Handle);
        
        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX, 0, PixelInternalFormat.Rgba, right.Size.Width, right.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, right.Data);
        GL.TexImage2D(TextureTarget.TextureCubeMapNegativeX, 0, PixelInternalFormat.Rgba, left.Size.Width, left.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, left.Data);
        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveY, 0, PixelInternalFormat.Rgba, top.Size.Width, top.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, top.Data);
        GL.TexImage2D(TextureTarget.TextureCubeMapNegativeY, 0, PixelInternalFormat.Rgba, bottom.Size.Width, bottom.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, bottom.Data);
        GL.TexImage2D(TextureTarget.TextureCubeMapNegativeZ, 0, PixelInternalFormat.Rgba, back.Size.Width, back.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, back.Data);
        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveZ, 0, PixelInternalFormat.Rgba, front.Size.Width, front.Size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, front.Data);

        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int) TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int) TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int) TextureWrapMode.ClampToEdge);
        
        GL.BindTexture(TextureTarget.TextureCubeMap, 0);
    }

    internal override void Bind(TextureUnit textureUnit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.TextureCubeMap, Handle);
    }

    internal override void Unbind()
    {
        GL.BindTexture(TextureTarget.TextureCubeMap, 0);
    }
}