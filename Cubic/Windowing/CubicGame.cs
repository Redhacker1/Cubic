using System;
using BepuPhysics;
using Cubic.Audio;
using Cubic.GUI;
using Cubic.Render;
using Cubic.Scenes;

namespace Cubic.Windowing;

public class CubicGame : IDisposable
{
    private GameSettings _settings;

    private ImGuiRenderer _imGuiRenderer;

    public ImGuiRenderer ImGui => _imGuiRenderer;

    public readonly GameWindow Window;
    internal Graphics GraphicsInternal;
    internal Simulation Simulation;

    protected Graphics Graphics => GraphicsInternal;
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

        GraphicsInternal = new Graphics(Window, _settings);
        Window.Visible = _settings.StartVisible;
        TargetFps = _settings.TargetFps;

        AudioDevice = new AudioDevice(_settings.AudioChannels);

        _imGuiRenderer = new ImGuiRenderer(GraphicsInternal);
        
        SetValues();

        Window.WindowMode = _settings.WindowMode;
        
        Input.Start(Window);

        Time.Start();
        
        UI.Initialize(Graphics.Viewport.Size);
        
        Initialize();

        while (!Window.ShouldClose)
        {
            AudioDevice.Update();
            if (Time.Stopwatch.Elapsed.TotalSeconds - Time.LastTime < _targetFrameDelta && _lockFps)
                continue;
            Input.Update(Window);
            Time.Update();
            Update();
            Metrics.Update();
            GraphicsInternal.PrepareFrame(SceneManager.Active.World.ClearColorInternal);
            Draw();
            GraphicsInternal.PresentFrame();
            if (Window.ShouldClose)
            {
                Window.ShouldClose = false;
                Close();
            }
        }
    }

    protected virtual void Initialize() => SceneManager.Initialize(this);

    protected virtual void Update()
    {
        UI.Update();
        _imGuiRenderer.Update(Time.DeltaTime);
        SceneManager.Update(this);
    }

    protected virtual void Draw()
    {
        SceneManager.Draw();
        UI.Draw(GraphicsInternal);
        _imGuiRenderer.Render();
    }

    public void Dispose()
    {
        Texture2D.Blank.Dispose();
        SceneManager.Active.Dispose();
        GraphicsInternal.Dispose();
        AudioDevice.Dispose();
    }

    public virtual void Close()
    {
        Window.ShouldClose = true;
    }

    private void SetValues()
    {
        Texture2D.Blank = new Texture2D(1, 1, new byte[] { 255, 255, 255, 255 }, false);
        Texture2D.Void = new Texture2D(1, 1, new byte[] { 0, 0, 0, 255 }, false);
    }
}