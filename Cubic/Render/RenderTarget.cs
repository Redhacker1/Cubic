using System;
using System.Drawing;
using Silk.NET.OpenGL;
using static Cubic.Render.Graphics;

namespace Cubic.Render;

public class RenderTarget : Texture
{
    internal uint Fbo;

    public unsafe RenderTarget(Size size, bool autoDispose) : base(autoDispose)
    {
        Fbo = Gl.GenFramebuffer();
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);

        Handle = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, Handle);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint) size.Width, (uint) size.Height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, null);
        Gl.BindTexture(TextureTarget.Texture2D, 0);

        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, Handle, 0);
        
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.Repeat);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.Repeat);

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        Size = size;
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

    public override void Dispose()
    {
        Gl.DeleteFramebuffer(Fbo);
        
        base.Dispose();
    }
}