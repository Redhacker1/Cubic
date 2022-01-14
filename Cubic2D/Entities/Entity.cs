using System;
using Cubic2D.Entities.Components;
using Cubic2D.Render;
using Cubic2D.Windowing;

namespace Cubic2D.Entities;

public class Entity
{
    public Transform Transform;

    private Component[] _components;
    private int _componentCount;

    public Entity() : this(new Transform()) { }

    public Entity(Transform transform)
    {
        Transform = transform;
        _components = new Component[5];
    }

    public void AddComponent(Component component)
    {
        foreach (Component comp in _components)
        {
            if (comp == null)
                continue;
            
            if (comp.GetType() == component.GetType())
                throw new CubicException("Entity can have only one type of each component.");
        }

        if (_componentCount + 1 > _components.Length)
            Array.Resize(ref _components, _components.Length * 2);
        _components[_componentCount] = component;
        _componentCount++;

        component.Entity = this;
        component.Initialize();
    }

    public Component GetComponent<T>() where T : Component
    {
        foreach (Component component in _components)
        {
            if (component.GetType() == typeof(T))
                return (T) component;
        }

        throw new CubicException("Requested component was not found.");
    }

    internal void Update()
    {
        foreach (Component component in _components)
            component?.Update();
    }

    internal void Draw(Graphics graphics)
    {
        foreach (Component component in _components)
            component?.Draw(graphics);
    }
}