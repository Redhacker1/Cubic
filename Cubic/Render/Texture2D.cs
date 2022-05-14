using System;
using System.Drawing;
using System.IO;
using Cubic.Utilities;
using Silk.NET.OpenGL;
using static Cubic.Render.Graphics;
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

    public unsafe void SetData(IntPtr data, int x, int y, int width, int height)
    {
        Gl.BindTexture(TextureTarget.Texture2D, Handle);
        Gl.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, (uint) width, (uint) height, PixelFormat.Rgba,
            PixelType.UnsignedByte, data.ToPointer());
        Gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public unsafe void SetData(byte[] data, int x, int y, int width, int height)
    {
        Gl.BindTexture(TextureTarget.Texture2D, Handle);
        fixed (byte* p = data)
            Gl.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, (uint) width, (uint) height, PixelFormat.Rgba, PixelType.UnsignedByte, p);
        Gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    private static unsafe uint CreateTexture(int width, int height, byte[] data, PixelFormat format = PixelFormat.Rgba)
    {
        uint texture = Gl.GenTexture();
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.Texture2D, texture);

        fixed (byte* p = data)
            Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint) width, (uint) height, 0, format, PixelType.UnsignedByte, p);

        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.Repeat);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.Repeat);
        
        Gl.GenerateMipmap(TextureTarget.Texture2D);
        
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxAnisotropy, 16);
        
        Gl.BindTexture(TextureTarget.Texture2D, 0);

        return texture;
    }
    
    internal override void Bind(TextureUnit textureUnit = TextureUnit.Texture0)
    {
        Gl.ActiveTexture(textureUnit);
        Gl.BindTexture(TextureTarget.Texture2D, Handle);
    }

    internal override void Unbind()
    {
        Gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public static Texture2D Blank { get; internal set; }

    public static Texture2D Void { get; internal set; }
}