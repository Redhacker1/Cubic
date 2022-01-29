using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Cubic2D.Windowing;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Cubic2D.Render;

public class Graphics : IDisposable
{
    public event OnResize ViewportResized;
    
    private Sdl2Window _window;
    
    internal readonly GraphicsDevice GraphicsDevice;
    internal readonly ResourceFactory ResourceFactory;

    // ReSharper disable once InconsistentNaming
    internal readonly CommandList CL;

    public readonly SpriteRenderer SpriteRenderer;

    public GraphicsApi Api
    {
        get
        {
            return GraphicsDevice.BackendType switch
            {
                GraphicsBackend.Direct3D11 => GraphicsApi.Direct3D,
                GraphicsBackend.Vulkan => GraphicsApi.Vulkan,
                GraphicsBackend.OpenGL => GraphicsApi.OpenGL,
                GraphicsBackend.Metal => GraphicsApi.Metal,
                GraphicsBackend.OpenGLES => GraphicsApi.OpenGLES,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public bool VSync
    {
        get => GraphicsDevice.SyncToVerticalBlank;
        set => GraphicsDevice.SyncToVerticalBlank = value;
    }

    internal Graphics(Sdl2Window window, GameSettings settings)
    {
        _window = window;
        GraphicsDeviceOptions options = new GraphicsDeviceOptions()
        {
            PreferDepthRangeZeroToOne = true,
            PreferStandardClipSpaceYDirection = true,
            SyncToVerticalBlank = settings.VSync,
            SwapchainDepthFormat = PixelFormat.R16_UNorm
        };

        GraphicsBackend backend;
        if (settings.Api == GraphicsApi.Default)
        {
            // Do no checks here cause if windows doesn't support DX11 then what drugs is it on
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                backend = GraphicsBackend.Direct3D11;
            // Try to support Vulkan in Linux if possible, cause it's better.
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)
                    ? GraphicsBackend.Vulkan
                    : GraphicsBackend.OpenGL;
            }
            // Newer macs should support Metal so use that where possible.
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal)
                    ? GraphicsBackend.Metal
                    : GraphicsBackend.OpenGL;
            }
            else
            {
                // OpenGL ES is fallback option, it should technically be supported on all platforms (untested)
                backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.OpenGL)
                    ? GraphicsBackend.OpenGL
                    : GraphicsBackend.OpenGLES;
            }
        }
        else
        {
            backend = settings.Api switch
            {
                GraphicsApi.Direct3D => GraphicsBackend.Direct3D11,
                GraphicsApi.Vulkan => GraphicsBackend.Vulkan,
                GraphicsApi.OpenGL => GraphicsBackend.OpenGL,
                GraphicsApi.OpenGLES => GraphicsBackend.OpenGLES,
                GraphicsApi.Metal => GraphicsBackend.Metal,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        try
        {
            GraphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options, backend);
        }
        catch (VeldridException)
        {
            // Multi-gpu linux issue where the GPU chosen may claim to support Vulkan but may not necessarily actually
            // support Vulkan (I had this issue). In this case, since Veldrid doesn't support manually selecting a 
            // device (Y E T), we fallback to OpenGL backend as it should work.
            if (backend == GraphicsBackend.Vulkan)
            {
                backend = GraphicsBackend.OpenGL;
                try
                {
                    GraphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options, backend);
                }
                catch (VeldridException)
                {
                    throw new CubicException(
                        "Fallback OpenGL graphics device could not be created! Please make sure your drivers are up to date.");
                }
            }
            else
                throw new CubicException(
                    "Graphics device with the given API could not be created. Please ensure the given API is supported on this platform, and that your drivers are up to date.");
        }

        ResourceFactory = GraphicsDevice.ResourceFactory;

        CL = ResourceFactory.CreateCommandList();
        
        window.Resized += WindowResized;

        SpriteRenderer = new SpriteRenderer(this);
    }
    

    internal void PrepareFrame(RgbaFloat clearColor)
    {
        // Prepare the command list so calls can be issued.
        CL.Begin();
        CL.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
        CL.ClearColorTarget(0, clearColor);
        CL.ClearDepthStencil(0);
    }

    internal void PresentFrame()
    {
        // End command list, and "present" to the main window.
        CL.End();
        GraphicsDevice.SubmitCommands(CL);
        GraphicsDevice.SwapBuffers();
        GraphicsDevice.WaitForIdle();
    }
    
    private void WindowResized()
    {
        // Resize viewport.
        GraphicsDevice.ResizeMainWindow((uint) _window.Width, (uint) _window.Height);
        ViewportResized?.Invoke(new Size(_window.Width, _window.Height));
    }

    public void Dispose()
    {
        // Dispose of the command list and graphics device, remove delegate for window resizing.
        CL.Dispose();
        GraphicsDevice.Dispose();
        _window.Resized -= WindowResized;
    }

    public delegate void OnResize(Size size);
}