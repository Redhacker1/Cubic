using Cubic2D.Windowing;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Cubic2D.Render;

public class Graphics : UnmanagedResource
{
    private Sdl2Window _window;
    
    internal readonly GraphicsDevice GraphicsDevice;
    internal readonly ResourceFactory ResourceFactory;

    // ReSharper disable once InconsistentNaming
    internal readonly CommandList CL;

    public readonly SpriteRenderer SpriteRenderer;

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

        // TODO: Add option in GameSettings to allow backend choice.
        GraphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options, GraphicsBackend.OpenGL);
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
    }

    internal override void Dispose()
    {
        // Dispose of the command list and graphics device, remove delegate for window resizing.
        CL.Dispose();
        GraphicsDevice.Dispose();
        _window.Resized -= WindowResized;
    }
}