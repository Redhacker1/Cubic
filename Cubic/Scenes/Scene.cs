using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cubic.Entities;
using Cubic.GUI;
using Cubic.Render;
using Cubic.Utilities;
using Cubic.Windowing;

namespace Cubic.Scenes;

public abstract class Scene : IDisposable
{
    internal readonly List<IDisposable> CreatedResources;

    private bool _updating;

    protected internal CubicGame Game { get; internal set; }
    protected Graphics Graphics => Game.GraphicsInternal;
    protected internal readonly World World;

    private readonly Dictionary<string, Entity> _entitiesQueue;
    private readonly Dictionary<string, Entity> _entities;
    private readonly Dictionary<string, Screen> _screens;
    private Queue<Screen> _screensToAdd;
    private List<Screen> _activeScreens;
    private int _popCount;

    protected Scene()
    {
        CreatedResources = new List<IDisposable>();
        _entities = new Dictionary<string, Entity>();
        _entitiesQueue = new Dictionary<string, Entity>();
        _screens = new Dictionary<string, Screen>();
        World = new World();
        _activeScreens = new List<Screen>();
        _screensToAdd = new Queue<Screen>();
    }

    protected internal virtual void Initialize() { }

    protected internal virtual void Update()
    {
        _updating = true;
        foreach (KeyValuePair<string, Entity> entity in _entities)
            entity.Value.Update();
        
        foreach (Screen screen in _activeScreens)
            screen.Update();
        _updating = false;

        foreach (KeyValuePair<string, Entity> ent in _entitiesQueue)
        {
            ent.Value.Initialize(Game);
            _entities.Add(ent.Key, ent.Value);
        }

        _entitiesQueue.Clear();
        
        while (_screensToAdd.TryDequeue(out Screen result))
            _activeScreens.Add(result);

        for (int i = 0; i < _popCount; i++)
            _activeScreens.RemoveAt(_activeScreens.Count - 1);
        _popCount = 0;
    }

    protected virtual void Unload() { }

    public void Dispose()
    {
        Unload();
        // Stop all sounds from playing.
        for (int i = 0; i < Game.AudioDevice.NumChannels; i++)
            Game.AudioDevice.Stop(i);
        
        foreach (Entity entity in _entities.Values)
            entity.Dispose();
        
        foreach (IDisposable resource in CreatedResources)
            resource.Dispose();
        
        World.Skybox?.Dispose();
    }

    /// <summary>
    /// Extend Cubic's graphics systems using this method.
    ///
    /// In order to get the engine to draw entities in the scene like normal, you <b>MUST</b> call base.Draw() somewhere
    /// within this method.
    /// </summary>
    protected internal virtual void Draw()
    {
        Camera.Main.GenerateViewMatrix();
        Camera2D.Main.GenerateTransformMatrix();
        World.Skybox?.Draw(Camera.Main);
        Graphics.SpriteRenderer.Begin(Camera2D.Main.TransformMatrix, World.SampleType);
        
        // Order the entities by their distance to the camera to support transparent sorting.
        foreach (KeyValuePair<string, Entity> entity in _entities.OrderBy(pair => -Vector3.Distance(pair.Value.Transform.Position, Camera.Main.Transform.Position)))
            entity.Value.Draw();
        Graphics.SpriteRenderer.End();
        
        foreach (Screen screen in _activeScreens)
            screen.Draw();
    }

    public void AddEntity(string name, Entity entity)
    {
        if (_updating)
        {
            _entitiesQueue.Add(name, entity);
            return;
        }
        entity.Initialize(Game);
        _entities.Add(name, entity);
    }

    public Entity GetEntity(string name) => _entities[name];

    public T GetEntity<T>(string name) where T : Entity => (T) _entities[name];
    
    public void AddScreen(Type screenType, string name, params object[] constructorParams)
    {
        if (screenType.BaseType != typeof(Screen))
            throw new CubicException($"Given screen must derive off {typeof(Screen)}");

        Screen screen = (Screen) Activator.CreateInstance(screenType, constructorParams);
        if (screen == null)
            throw new CubicException("Screen was not created.");
        screen.Game = Game;
        screen.Initialize();
        _screens.Add(name, screen);
    }

    public void OpenScreen(string name)
    {
        Screen screen = _screens[name];
        screen.Open();
        if (_updating)
            _screensToAdd.Enqueue(screen);
        else
            _activeScreens.Add(screen);
    }

    public void CloseScreen()
    {
        _activeScreens[^1].Close();
        _popCount++;
    }

    internal void CloseScreenInternal()
    {
        _popCount++;
    }
}