using System;
using Cubic2D.Audio;
using Cubic2D.Render;
using Cubic2D.Scenes;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cubic2D.Windowing;

public sealed unsafe class CubicGame : IDisposable
{
    private GameSettings _settings;
    
    public readonly GameWindow Window;
    internal Graphics Graphics;
    public AudioDevice AudioDevice { get; private set; }

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

        Graphics = new Graphics(Window, _settings);
        Window.Visible = _settings.StartVisible;
        TargetFps = _settings.TargetFps;

        AudioDevice = new AudioDevice(_settings.AudioChannels);

        SceneManager.Initialize(this);

        Window.WindowMode = _settings.WindowMode;
        
        Time.Start();
        
        while (!Window.ShouldClose)
        {
            if (Time.Stopwatch.Elapsed.TotalSeconds - Time.LastTime < _targetFrameDelta && _lockFps)
                continue;
            Input.Update(Window);
            Time.Update();
            AudioDevice.Update();
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
        AudioDevice.Dispose();
    }

    public void Close()
    {
        Window.ShouldClose = true;
    }
}