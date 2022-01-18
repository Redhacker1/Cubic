using System;
using Cubic2D.Render;
using Cubic2D.Scenes;
using Veldrid;

namespace Cubic2D.Windowing;

public sealed class CubicGame : IDisposable
{
    private GameSettings _settings;
    
    public readonly GameWindow Window;
    internal Graphics Graphics;

    private bool _running;
    private bool _lockFps;
    private float _targetFrameDelta;

    public uint TargetFps
    {
        get => (uint) (1 / _targetFrameDelta);
        set
        {
            if (value == 0)
                _lockFps = false;
            else
            {
                _lockFps = true;
                _targetFrameDelta = 1f / value;
            }
        }
    }

    public CubicGame(GameSettings settings)
    {
        _settings = settings;
        Window = new GameWindow(settings);
        //Current = this;
    }

    public void Run()
    {
        if (_running)
            throw new CubicException("Cubic cannot run multiple game instances at the same time.");
        
        Window.Prepare();

        Graphics = new Graphics(Window.SdlWindow, _settings);
        Window.SdlWindow.Visible = _settings.StartVisible;
        TargetFps = _settings.TargetFps;
        
        SetDefaults();
        
        SceneManager.Initialize(this);
        
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
            if (Time.Stopwatch.Elapsed.TotalSeconds - Time.LastTime < _targetFrameDelta && _lockFps)
                continue;
            Time.Update();
            Input.Update(Window.SdlWindow.PumpEvents());
            SceneManager.Update(this);
            Graphics.PrepareFrame(SceneManager.Active.World.ClearColorInternal);
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

    private void SetDefaults()
    {
        // I really hate having to do this but I wanna avoid static stuff.
        Texture2D.Blank = new Texture2D(this, 1, 1, new byte[] {255, 255, 255, 255});
    }
    
    //public static CubicGame Current { get; private set; }
}