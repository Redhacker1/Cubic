using System;
using System.Drawing;
using System.IO;
using Cubic.Utilities;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace Cubic.Render;

public class Texture2D : Texture
{
    public Texture2D(string path, bool autoDispose = true) : base(autoDispose)
    {
        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(path));
            
        Handle = CreateTexture(result.Width, result.Height, result.Data,
            result.Comp == ColorComponents.RedGreenBlueAlpha ? PixelFormat.Rgba : PixelFormat.Rgb);
        Size = new Size(result.Width, result.Height);
    }

    public Texture2D(int width, int height, byte[] data, bool autoDispose = true) : base(autoDispose)
    {
        Handle = CreateTexture(width, height, data);
        Size = new Size(width, height);
    }

    public Texture2D(int width, int height, bool autoDispose = true) : base(autoDispose)
    {
        Handle = CreateTexture(width, height, null);
        Size = new Size(width, height);
    }

    public Texture2D(Bitmap bitmap, bool autoDispose = true) : base(autoDispose)
    {
        Handle = CreateTexture(bitmap.Size.Width, bitmap.Size.Height, bitmap.Data);
        Size = bitmap.Size;
    }

    public void SetData(IntPtr data, int x, int y, int width, int height)
    {
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, width, height, PixelFormat.Rgba,
            PixelType.UnsignedByte, data);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void SetData(byte[] data, int x, int y, int width, int height)
    {
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, width, height, PixelFormat.Rgba,
            PixelType.UnsignedByte, data);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    private static int CreateTexture(int width, int height, byte[] data, PixelFormat format = PixelFormat.Rgba)
    {
        int texture = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, texture);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, format,
            PixelType.UnsignedByte, data);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.Repeat);
        
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        
        GL.BindTexture(TextureTarget.Texture2D, 0);

        return texture;
    }

    public static Texture2D Blank { get; internal set; }
}