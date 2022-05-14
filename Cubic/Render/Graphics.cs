using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Cubic.Utilities;
using Cubic.Windowing;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Cubic.Render;

public class Graphics : IDisposable
{
    public event OnResize ViewportResized;
    
    private GameWindow _window;

    public readonly SpriteRenderer SpriteRenderer;

    private Rectangle _viewport;

    internal static GL Gl;

    public bool VSync
    {
        set => GameWindow.GLFW.SwapInterval(value ? 1 : 0);
    }

    public Rectangle Viewport
    {
        get => _viewport;
        set
        {
            _viewport = value;
            Gl.Viewport(value.X, value.Y, (uint) value.Width, (uint) value.Height);
            ViewportResized?.Invoke(value.Size);
        }
    }

    public void SetRenderTarget(RenderTarget target)
    {
        if (target == null)
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Viewport = new Rectangle(0, 0, _window.Size.Width, _window.Size.Height);
            return;
        }

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.Fbo);
        Viewport = new Rectangle(0, 0, target.Size.Width, target.Size.Height);
    }

    public void Clear(Vector4 clearColor)
    {
        Gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        Gl.Clear((uint) ClearBufferMask.ColorBufferBit | (uint) ClearBufferMask.DepthBufferBit);
    }

    public void Clear(Color clearColor)
    {
        Gl.ClearColor(clearColor);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    internal unsafe Graphics(GameWindow window, GameSettings settings)
    {
        _window = window;

        window.Resize += WindowResized;
        
        Gl = GL.GetApi(new GlfwContext(GameWindow.GLFW, window.Handle));

        VSync = settings.VSync;

        Viewport = new Rectangle(0, 0, window.Size.Width, window.Size.Height);
        
        SpriteRenderer = new SpriteRenderer(this);
        
        Gl.Enable(EnableCap.Blend);
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        Gl.Enable(EnableCap.CullFace);
        Gl.CullFace(CullFaceMode.Back);
        Gl.FrontFace(FrontFaceDirection.CW);
        
        Gl.Enable(EnableCap.DepthTest);
        Gl.DepthFunc(DepthFunction.Lequal);
        
        //Gl.Enable(EnableCap.ScissorTest);
        //SetScissor(Viewport);
        
        if (settings.MsaaSamples > 0)
            Gl.Enable(EnableCap.Multisample);
    }
    

    internal void PrepareFrame(Vector4 clearColor)
    {
        Clear(clearColor);
        SetScissor(Viewport);
    }

    internal unsafe void PresentFrame()
    {
        GameWindow.GLFW.SwapBuffers(_window.Handle);
    }
    
    private void WindowResized(Size size)
    {
        // Resize viewport.
        Viewport = new Rectangle(0, 0, size.Width, size.Height);
    }

    /*public Bitmap Capture(Rectangle region)
    {
        byte[] upsideDownData = new byte[region.Width * region.Height * 3];
        Gl.ReadPixels(0, 0, region.Width, region.Height, PixelFormat.Rgb, PixelType.UnsignedByte, upsideDownData);
        // We need to reverse the data as it's stored upside down because OpenGl
        // We also convert from RGB to RGBA too.
        byte[] data = new byte[region.Width * region.Height * 4];
        for (int x = 0; x < region.Width; x++)
        {
            for (int y = 0; y < region.Height; y++)
            {
                data[(y * region.Width + x) * 4] = upsideDownData[((region.Height - 1 - y) * region.Width + x) * 3];
                data[(y * region.Width + x) * 4 + 1] = upsideDownData[((region.Height - 1 - y) * region.Width + x) * 3 + 1];
                data[(y * region.Width + x) * 4 + 2] = upsideDownData[((region.Height - 1 - y) * region.Width + x) * 3 + 2];
                data[(y * region.Width + x) * 4 + 3] = 255;
            }
        }
        return new Bitmap(region.Width, region.Height, data);
    }

    public Bitmap Capture() => Capture(Viewport);*/

    public void SetScissor(Rectangle rectangle)
    {
        Gl.Scissor(rectangle.X, Viewport.Height - rectangle.Height - rectangle.Y, (uint) rectangle.Width, (uint) rectangle.Height);
    }

    /*public RenderObject CreateRenderObject()
    {
        int vao = Gl.GenVertexArray();
        RenderObject obj = new RenderObject(vao);
        return obj;
    }

    public Buffer CreateBuffer(BufferType type, int size)
    {
        int buf = Gl.GenBuffer();
        Buffer buffer = new Buffer(buf,
            (type & BufferType.VertexBuffer) == BufferType.VertexBuffer
                ? BufferTarget.ArrayBuffer
                : BufferTarget.ElementArrayBuffer);

        Gl.BufferData(buffer.Target, size, IntPtr.Zero,
            (type & BufferType.Dynamic) == BufferType.Dynamic
                ? BufferUsageHint.DynamicDraw
                : BufferUsageHint.StaticDraw);

        return buffer;
    }

    public void UpdateBuffer<T>(Buffer buffer, int offset, T[] data) where T : struct
    {
        Gl.BindBuffer(buffer.Target, buffer.Handle);
        Gl.BufferSubData(buffer.Target, new IntPtr(offset), data.Length * Marshal.SizeOf<T>(), data);
    }

    public void BindBufferToRenderObject(RenderObject obj, Buffer buffer)
    {
        Gl.BindVertexArray(obj.Handle);
        Gl.BindBuffer(buffer.Target, buffer.Handle);
        Gl.BindVertexArray(0);
        Gl.BindBuffer(buffer.Target, 0);
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