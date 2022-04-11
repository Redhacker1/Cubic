using System;
using Cubic.Render;
using Cubic.Windowing;

namespace Cubic.Entities.Components;

public abstract class Component
{
    public bool Enabled = true;
    
    protected internal Entity Entity { get; internal set; }

    protected CubicGame Game => Entity.Game;
    protected Graphics Graphics => Game.GraphicsInternal;

    protected Transform Transform => Entity.Transform;
    
    protected internal virtual void Initialize() { }

    protected internal virtual void Update() { }

    protected internal virtual void Draw() { }

    protected internal virtual void Unload() { }

    protected T GetComponent<T>() where T : Component => Entity.GetComponent<T>();

    protected void AddComponent(Type component, params object[] args) => Entity.AddComponent(component, args);

    protected void RemoveComponent(Type component) => Entity.RemoveComponent(component);
}