using System.Collections.Generic;
using Cubic2D.Render;

namespace Cubic2D.Scenes;

public abstract class Scene : UnmanagedResource
{
    internal readonly List<UnmanagedResource> CreatedResources;

    protected Scene()
    {
        CreatedResources = new List<UnmanagedResource>();
    }

    protected internal virtual void Initialize() { }

    protected internal virtual void Update() { }

    protected virtual void Unload() { }

    internal override void Dispose()
    {
        Unload();
        
        foreach (UnmanagedResource resource in CreatedResources)
            resource.Dispose();
    }

    internal void Draw()
    {
        
    }
}