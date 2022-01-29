using System;
using Cubic2D.Render;
using Cubic2D.Windowing;

namespace Cubic2D.Entities.Components;

public abstract class Component
{
    protected internal CubicGame Game { get; internal set; }
    protected internal Entity Entity { get; internal set; }

    protected Transform Transform => Entity.Transform;
    
    protected internal virtual void Initialize() { }

    protected internal virtual void Update() { }

    protected internal virtual void Draw(Graphics graphics) { }

    protected T GetComponent<T>() where T : Component => (T) Entity.GetComponent<T>();

    protected void AddComponent(Type component, params object[] args) => Entity.AddComponent(component, args);

    protected void RemoveComponent(Type component) => Entity.RemoveComponent(component);
}