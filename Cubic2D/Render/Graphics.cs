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

    public Rectangle Viewport => new Rectangle(_viewport[0], _viewport[1], _viewport[2], _viewport[3]);

    internal Graphics(GameWindow window, GameSettings settings)
    {
        _window = window;

        window.Resize += WindowResized;
        
        GL.LoadBindings(new GLFWBindingsContext());

        VSync = settings.VSync;

        _viewport = new int[4];
        
        GL.GetInteger(GetPName.Viewport, _viewport);

        //GL.Enable(EnableCap.ScissorTest);
        
        SpriteRenderer = new SpriteRenderer(this);
    }
    

    internal void PrepareFrame(Vector4 clearColor)
    {
        GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
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
        GL.GetInteger(GetPName.Viewport, _viewport);
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