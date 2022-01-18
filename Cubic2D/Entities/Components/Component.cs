using Cubic2D.Render;
using Cubic2D.Windowing;

namespace Cubic2D.Entities.Components;

public abstract class Component
{
    protected internal Entity Entity { get; internal set; }

    protected Transform Transform => Entity.Transform;
    
    protected internal virtual void Initialize(CubicGame game) { }

    protected internal virtual void Update(CubicGame game) { }

    protected internal virtual void Draw(Graphics graphics, CubicGame game) { }

    protected T GetComponent<T>() where T : Component => (T) Entity.GetComponent<T>();

    protected void AddComponent(CubicGame game, Component component) => Entity.AddComponent(game, component);
}