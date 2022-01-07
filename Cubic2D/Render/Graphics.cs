using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Cubic2D.Render;

public class Graphics
{
    internal readonly GraphicsDevice GraphicsDevice;
    internal readonly ResourceFactory ResourceFactory;

    // ReSharper disable once InconsistentNaming
    internal readonly CommandList CL;

    internal Graphics(Sdl2Window window)
    {
        GraphicsDeviceOptions options = new GraphicsDeviceOptions()
        {
            PreferDepthRangeZeroToOne = true,
            PreferStandardClipSpaceYDirection = true
        };

        // TODO: Add option in GameSettings to allow backend choice.
        GraphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options, GraphicsBackend.OpenGL);
        ResourceFactory = GraphicsDevice.ResourceFactory;

        CL = ResourceFactory.CreateCommandList();
    }

    internal void PrepareFrame()
    {
        CL.Begin();
        CL.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
        CL.ClearColorTarget(0, RgbaFloat.CornflowerBlue);
    }

    internal void PresentFrame()
    {
        CL.End();
        GraphicsDevice.SubmitCommands(CL);
        GraphicsDevice.SwapBuffers();
        GraphicsDevice.WaitForIdle();
    }

    internal void Dispose()
    {
        CL.Dispose();
        GraphicsDevice.Dispose();
    }
}