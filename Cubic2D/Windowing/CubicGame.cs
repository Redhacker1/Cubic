using System;
using Cubic2D.Render;
using Cubic2D.Scenes;

namespace Cubic2D.Windowing;

public sealed class CubicGame : IDisposable
{
    private GameSettings _settings;
    
    public readonly GameWindow Window;
    public Graphics Graphics { get; private set; }

    public CubicGame(GameSettings settings)
    {
        _settings = settings;
        Window = new GameWindow(settings);
        Current = this;
    }

    public void Run()
    {
        Window.Prepare();

        Graphics = new Graphics(Window.SdlWindow, _settings);
        
        SceneManager.Initialize();
        
        Window.SdlWindow.Visible = true;
        
        while (Window.SdlWindow.Exists)
        {
            Input.Update(Window.SdlWindow.PumpEvents());
            SceneManager.Update();
            Graphics.PrepareFrame();
            SceneManager.Draw();
            Graphics.PresentFrame();
        }
    }

    public void Dispose()
    {
        Graphics.Dispose();
    }
    
    public static CubicGame Current { get; private set; }
}