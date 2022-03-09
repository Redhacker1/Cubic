using System;
using Cubic2D.Audio;
using Cubic2D.GUI;
using Cubic2D.Render;
using Cubic2D.Scenes;

namespace Cubic2D.Windowing;

public class CubicGame : IDisposable
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
    }

    public void Run()
    {
        if (_running)
            throw new CubicException("Cubic cannot run multiple game instances at the same time.");
        _running = true;
        
        Window.Prepare();

        Graphics = new Graphics(Window, _settings);
        Window.Visible = _settings.StartVisible;
        TargetFps = _settings.TargetFps;

        AudioDevice = new AudioDevice(_settings.AudioChannels);
        
        SetValues();
        
        Initialize();

        Window.WindowMode = _settings.WindowMode;
        
        Time.Start();
        
        while (!Window.ShouldClose)
        {
            if (Time.Stopwatch.Elapsed.TotalSeconds - Time.LastTime < _targetFrameDelta && _lockFps)
                continue;
            Input.Update(Window);
            Time.Update();
            AudioDevice.Update();
            UI.Update();
            Update();
            Metrics.Update();
            Graphics.PrepareFrame(SceneManager.Active.World.ClearColorInternal);
            Draw(Graphics);
            UI.Draw(Graphics);
            Graphics.PresentFrame();
        }
    }

    protected virtual void Initialize() => SceneManager.Initialize(this);

    protected virtual void Update() => SceneManager.Update(this);

    protected virtual void Draw(Graphics graphics) => SceneManager.Draw();

    public void Dispose()
    {
        Texture2D.Blank.Dispose();
        SceneManager.Active.Dispose();
        Graphics.Dispose();
        AudioDevice.Dispose();
    }

    public void Close()
    {
        Window.ShouldClose = true;
    }

    private void SetValues()
    {
        Texture2D.Blank = new Texture2D(1, 1, new byte[] { 255, 255, 255, 255 }, false);
    }
}