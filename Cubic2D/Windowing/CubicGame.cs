using System;
using Cubic2D.Render;

namespace Cubic2D.Windowing;

public sealed class CubicGame : IDisposable
{
    public readonly GameWindow Window;
    public Graphics Graphics { get; private set; }

    public CubicGame(GameSettings settings)
    {
        Window = new GameWindow(settings);
        Current = this;
    }

    public void Run()
    {
        Window.Prepare();

        Graphics = new Graphics(Window.SdlWindow);
        
        // TODO: Initialise scenes etc etc etc

        Window.CenterWindow();
        Window.SdlWindow.Visible = true;
        
        while (Window.SdlWindow.Exists)
        {
            Window.SdlWindow.PumpEvents();
            // todo Update
            Graphics.PrepareFrame();
            // todo Draw
            Graphics.PresentFrame();
        }
    }

    public void Dispose()
    {
        Graphics.Dispose();
    }
    
    public static CubicGame Current { get; private set; }
}