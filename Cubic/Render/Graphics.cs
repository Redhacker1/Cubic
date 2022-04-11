using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Cubic.Utilities;
using Cubic.Windowing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cubic.Render;

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
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void Clear(Color clearColor)
    {
        GL.ClearColor(clearColor);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
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
        
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
        GL.FrontFace(FrontFaceDirection.Cw);
        
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
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

    public Bitmap Capture(Rectangle region)
    {
        byte[] upsideDownData = new byte[region.Width * region.Height * 4];
        GL.ReadPixels(0, 0, region.Width, region.Height, PixelFormat.Rgba, PixelType.UnsignedByte, upsideDownData);
        // We need to reverse the data as it's stored upside down because OpenGL
        byte[] data = new byte[region.Width * region.Height * 4];
        for (int x = 0; x < region.Width; x++)
        {
            for (int y = 0; y < region.Height; y++)
            {
                data[(y * region.Width + x) * 4] = upsideDownData[((region.Height - 1 - y) * region.Width + x) * 4];
                data[(y * region.Width + x) * 4 + 1] = upsideDownData[((region.Height - 1 - y) * region.Width + x) * 4 + 1];
                data[(y * region.Width + x) * 4 + 2] = upsideDownData[((region.Height - 1 - y) * region.Width + x) * 4 + 2];
                data[(y * region.Width + x) * 4 + 3] = upsideDownData[((region.Height - 1 - y) * region.Width + x) * 4 + 3];
            }
        }
        return new Bitmap(region.Width, region.Height, data);
    }

    public Bitmap Capture() => Capture(Viewport);

    /*public void SetScissor(Rectangle rectangle)
    {
        GL.Scissor(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    public void ResetScissor()
    {
        GL.Scissor(0, 0, FramebufferSize.Width, FramebufferSize.Height);
    }*/

    /*public RenderObject CreateRenderObject()
    {
        int vao = GL.GenVertexArray();
        RenderObject obj = new RenderObject(vao);
        return obj;
    }

    public Buffer CreateBuffer(BufferType type, int size)
    {
        int buf = GL.GenBuffer();
        Buffer buffer = new Buffer(buf,
            (type & BufferType.VertexBuffer) == BufferType.VertexBuffer
                ? BufferTarget.ArrayBuffer
                : BufferTarget.ElementArrayBuffer);

        GL.BufferData(buffer.Target, size, IntPtr.Zero,
            (type & BufferType.Dynamic) == BufferType.Dynamic
                ? BufferUsageHint.DynamicDraw
                : BufferUsageHint.StaticDraw);

        return buffer;
    }

    public void UpdateBuffer<T>(Buffer buffer, int offset, T[] data) where T : struct
    {
        GL.BindBuffer(buffer.Target, buffer.Handle);
        GL.BufferSubData(buffer.Target, new IntPtr(offset), data.Length * Marshal.SizeOf<T>(), data);
    }

    public void BindBufferToRenderObject(RenderObject obj, Buffer buffer)
    {
        GL.BindVertexArray(obj.Handle);
        GL.BindBuffer(buffer.Target, buffer.Handle);
        GL.BindVertexArray(0);
        GL.BindBuffer(buffer.Target, 0);
    }*/

    public void Dispose()
    {
        SpriteRenderer.Dispose();
        // Dispose of the command list and graphics device, remove delegate for window resizing.
        _window.Resize -= WindowResized;
    }

    public delegate void OnResize(Size size);
}

public enum BufferType
{
    VertexBuffer = 1 << 1,
    IndexBuffer = 1 << 2,
    
    Dynamic = 1 << 3
}