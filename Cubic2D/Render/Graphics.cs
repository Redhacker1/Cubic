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

    public bool VSync
    {
        set => GLFW.SwapInterval(value ? 1 : 0);
    }

    public Size FramebufferSize
    {
        get
        {
            int[] data = new int[4];
            GL.GetInteger(GetPName.Viewport, data);
            return new Size(data[2], data[3]);
        }
    }

    internal Graphics(GameWindow window, GameSettings settings)
    {
        _window = window;

        window.Resize += WindowResized;
        
        GL.LoadBindings(new GLFWBindingsContext());

        VSync = settings.VSync;

        SpriteRenderer = new SpriteRenderer(this);
    }
    

    internal void PrepareFrame(Vector4 clearColor)
    {
        GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
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

    public void Dispose()
    {
        SpriteRenderer.Dispose();
        // Dispose of the command list and graphics device, remove delegate for window resizing.
        _window.Resize -= WindowResized;
    }

    public delegate void OnResize(Size size);
}