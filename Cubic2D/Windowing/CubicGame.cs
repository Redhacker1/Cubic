using System;
using Cubic2D.Render;
using Cubic2D.Scenes;
using Veldrid;

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
        Window.SdlWindow.WindowState = _settings.WindowMode switch
        {
            WindowMode.Windowed => WindowState.Normal,
            WindowMode.Fullscreen => WindowState.FullScreen,
            WindowMode.BorderlessFullscreen => WindowState.BorderlessFullScreen,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        Time.Start();
        
        while (Window.SdlWindow.Exists)
        {
            Time.Update();
            Input.Update(Window.SdlWindow.PumpEvents());
            SceneManager.Update();
            Graphics.PrepareFrame();
            SceneManager.Draw();
            Graphics.PresentFrame();
        }
    }

    public void Dispose()
    {
        SceneManager.Active.Dispose();
        Graphics.Dispose();
    }

    public void Close()
    {
        Window.SdlWindow.Close();
    }
    
    public static CubicGame Current { get; private set; }
}