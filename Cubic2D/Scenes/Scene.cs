using System;
using System.Collections.Generic;
using Cubic2D.Entities;
using Cubic2D.Render;
using Cubic2D.Utilities;
using Cubic2D.Windowing;

namespace Cubic2D.Scenes;

public abstract class Scene : IDisposable
{
    internal readonly List<IDisposable> CreatedResources;

    protected internal Graphics Graphics { get; internal set; }
    protected internal readonly World World;

    private readonly Dictionary<string, Entity> _entities;
    private int _entityCount;
    
    protected internal CubicGame Game { get; internal set; }

    protected Scene()
    {
        CreatedResources = new List<IDisposable>();
        _entities = new Dictionary<string, Entity>();
        World = new World();
        Camera main = new Camera();
        Camera.Main = main;
        _entities.Add("Main Camera", main);
    }

    protected internal virtual void Initialize() { }

    protected internal virtual void Update()
    {
        foreach (KeyValuePair<string, Entity> entity in _entities)
            entity.Value.Update();
    }

    protected virtual void Unload() { }

    public void Dispose()
    {
        Unload();
        
        foreach (IDisposable resource in CreatedResources)
            resource.Dispose();
    }

    /// <summary>
    /// Extend Cubic's graphics systems using this method.
    ///
    /// In order to get the engine to draw entities in the scene like normal, you <b>MUST</b> call base.Draw() somewhere
    /// within this method.
    /// </summary>
    protected internal virtual void Draw()
    {
        Camera.Main.GenerateTransformMatrix();
        Graphics.SpriteRenderer.Begin(Camera.Main.TransformMatrix);
        foreach (KeyValuePair<string, Entity> entity in _entities)
            entity.Value.Draw(Graphics);
        Graphics.SpriteRenderer.End();
    }

    public void AddEntity(string name, Entity entity)
    {
        entity.Initialize(Game);
        _entities.Add(name, entity);
    }

    public Entity GetEntity(string name) => _entities[name];

    public T GetEntity<T>(string name) where T : Entity => (T) _entities[name];
}