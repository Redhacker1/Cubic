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

    private readonly int[] _viewport;

    public bool VSync
    {
        set => GLFW.SwapInterval(value ? 1 : 0);
    }

    public Rectangle Viewport
    {
        get
        {
            GL.GetInteger(GetPName.Viewport, _viewport);
            return new Rectangle(_viewport[0], _viewport[1], _viewport[2], _viewport[3]);
        }
    }

    public void SetRenderTarget(RenderTarget target)
    {
        if (target == null)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, _window.Size.Width, _window.Size.Height);
            return;
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, target.Fbo);
        GL.Viewport(0, 0, target.Size.Width, target.Size.Height);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    internal Graphics(GameWindow window, GameSettings settings)
    {
        _window = window;

        window.Resize += WindowResized;
        
        GL.LoadBindings(new GLFWBindingsContext());

        VSync = settings.VSync;

        _viewport = new int[4];

        //GL.Enable(EnableCap.ScissorTest);
        
        SpriteRenderer = new SpriteRenderer(this);
    }
    

    internal void PrepareFrame(Vector4 clearColor)
    {
        GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        //ResetScissor();
    }

    internal unsafe void PresentFrame()
    {
        GLFW.SwapBuffers(_window.Handle);
    }
    
    private void WindowResized(Size size)
    {
        // Resize viewport.
        GL.Viewport(0, 0, size.Width, size.Height);
        ViewportResized?.Invoke(size);
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