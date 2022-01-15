using System.Collections.Generic;
using Cubic2D.Entities;
using Cubic2D.Render;
using Cubic2D.Utilities;

namespace Cubic2D.Scenes;

public abstract class Scene : UnmanagedResource
{
    internal readonly List<UnmanagedResource> CreatedResources;

    protected internal Graphics Graphics { get; internal set; }
    protected internal readonly World World;

    protected readonly Dictionary<string, Entity> Entities;
    private int _entityCount;

    protected Scene()
    {
        CreatedResources = new List<UnmanagedResource>();
        Entities = new Dictionary<string, Entity>();
        World = new World();
        Camera main = new Camera();
        Camera.Main = main;
        Entities.Add("Main Camera", main);
    }

    protected internal virtual void Initialize() { }

    protected internal virtual void Update()
    {
        foreach (KeyValuePair<string, Entity> entity in Entities)
            entity.Value.Update();
    }

    protected virtual void Unload() { }

    internal override void Dispose()
    {
        Unload();
        
        foreach (UnmanagedResource resource in CreatedResources)
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
        foreach (KeyValuePair<string, Entity> entity in Entities)
            entity.Value.Draw(Graphics);
        Graphics.SpriteRenderer.End();
    }
}