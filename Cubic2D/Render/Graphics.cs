using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Cubic2D.Windowing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cubic2D.Render;

public class Graphics : IDisposable
{
    public event OnResize ViewportResized;
    
    private GameWindow _window;

    public readonly SpriteRenderer SpriteRenderer;

    private Rectangle _viewport;

    public bool VSync
    {
        set => GLFW.SwapInterval(value ? 1 : 0);
    }

    public Rectangle Viewport
    {
        get => _viewport;
        set
        {
            _viewport = value;
            GL.Viewport(value.X, value.Y, value.Width, value.Height);
            ViewportResized?.Invoke(value.Size);
        }
    }

    public void SetRenderTarget(RenderTarget target)
    {
        if (target == null)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Viewport = new Rectangle(0, 0, _window.Size.Width, _window.Size.Height);
            return;
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, target.Fbo);
        Viewport = new Rectangle(0, 0, target.Size.Width, target.Size.Height);
    }

    public void Clear(Vector4 clearColor)
    {
        GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void Clear(Color clearColor)
    {
        GL.ClearColor(clearColor);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    internal Graphics(GameWindow window, GameSettings settings)
    {
        _window = window;

        window.Resize += WindowResized;
        
        GL.LoadBindings(new GLFWBindingsContext());

        VSync = settings.VSync;

        //GL.Enable(EnableCap.ScissorTest);

        Viewport = new Rectangle(0, 0, window.Size.Width, window.Size.Height);
        
        SpriteRenderer = new SpriteRenderer(this);
    }
    

    internal void PrepareFrame(Vector4 clearColor)
    {
        Clear(clearColor);
        //ResetScissor();
    }

    internal unsafe void PresentFrame()
    {
        GLFW.SwapBuffers(_window.Handle);
    }
    
    private void WindowResized(Size size)
    {
        // Resize viewport.
        Viewport = new Rectangle(0, 0, size.Width, size.Height);
    }

    /*public void SetScissor(Rectangle rectangle)
    {
        GL.Scissor(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    public void ResetScissor()
    {
        GL.Scissor(0, 0, FramebufferSize.Width, FramebufferSize.Height);
    }*/

    public void Dispose()
    {
        SpriteRenderer.Dispose();
        // Dispose of the command list and graphics device, remove delegate for window resizing.
        _window.Resize -= WindowResized;
    }

    public delegate void OnResize(Size size);
}