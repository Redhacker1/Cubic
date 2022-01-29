using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cubic2D.Entities.Components;
using Cubic2D.Render;
using Cubic2D.Windowing;

namespace Cubic2D.Entities;

public class Entity
{
    private CubicGame _game;
    private bool _initialized;
    private bool _updating;
    
    public Transform Transform;
    
    private Component[] _components;
    private List<ComponentState> _componentStates;
    private int _componentCount;

    public Entity() : this(new Transform()) { }

    public Entity(Transform transform)
    {
        Transform = transform;
        _components = new Component[5];
        _componentStates = new List<ComponentState>();
    }

    /// <summary>
    /// Add a component to this entity.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    /// <exception cref="CubicException"></exception>
    public void AddComponent(Type component, params object[] args)
    {
        if (component != typeof(Component) && component.BaseType != typeof(Component))
            throw new Exception($"Given component must be of type {typeof(Component)}");
        
        foreach (Component comp in _components)
        {
            if (comp == null)
                continue;
            
            if (comp.GetType() == component)
                throw new CubicException("Entity can have only one type of each component.");
        }

        // Parameter checking.
        bool correctArgs = false;
        // Get all constructors the component has
        ConstructorInfo[] constructors = component.GetConstructors();
        foreach (ConstructorInfo info in constructors)
        {
            ParameterInfo[] parameters = info.GetParameters();
            // If a matching number of parameters is found...
            if (args.Length == parameters.Length)
            {
                correctArgs = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != args[i].GetType())
                        correctArgs = false;
                }

                break;
            }
        }

        if (!correctArgs)
        {
            string msg =
                $"Attempted to call constructor of type \"{component}\" however none matched provided parameters.\nThe following constructor(s) are available:\n";
            foreach (ConstructorInfo info in constructors)
            {
                ParameterInfo[] parameters = info.GetParameters();
                msg += $"\t{component.Name}(";
                for (int i = 0; i < parameters.Length; i++)
                {
                    msg += $"{parameters[i].ParameterType.Name} {parameters[i].Name}";
                    if (i < parameters.Length - 1)
                        msg += ", ";
                }

                msg += ")\n";
            }

            throw new CubicException(msg);
        }

        if (_updating)
            _componentStates.Add(new ComponentState(component, true, args));
        else
            CreateComponent(component, args);
    }

    public void RemoveComponent(Type component)
    {
        if (component != typeof(Component) && component.BaseType != typeof(Component))
            throw new Exception($"Given component must be of type {typeof(Component)}");

        if (_updating)
        {
            _componentStates.Add(new ComponentState(component, false, null));
            return;
        }

        DeleteComponent(component);
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

    protected internal virtual void Update()
    {
        _updating = true;
        foreach (Component component in _components)
            component?.Update();
        _updating = false;

        foreach (ComponentState cState in _componentStates)
        {
            if (cState.Add)
                CreateComponent(cState.Component, cState.Args);
            else
                DeleteComponent(cState.Component);
        }
        _componentStates.Clear();
    }

    protected internal virtual void Draw(Graphics graphics)
    {
        foreach (Component component in _components)
            component?.Draw(graphics);
    }

    internal void Initialize(CubicGame game)
    {
        _game = game;
        
        foreach (Component comp in _components)
        {
            if (comp == null)
                continue;
            comp.Entity = this;
            comp.Game = _game;
            comp.Initialize();
        }

        _initialized = true;
    }

    private void CreateComponent(Type component, object[] args)
    {
        Component comp = (Component) Activator.CreateInstance(component, args);
        if (comp == null)
            throw new CubicException("Component could not be created.");
        
        if (_componentCount + 1 > _components.Length)
            Array.Resize(ref _components, _components.Length * 2);
        _components[_componentCount] = comp;
        _componentCount++;

        if (!_initialized)
            return;
        
        comp.Entity = this;
        comp.Game = _game;
        comp.Initialize();
    }

    private void DeleteComponent(Type component)
    {
        for (int i = 0; i < _components.Length; i++)
        {
            if (_components[i]?.GetType() == component)
            {
                _componentCount--;
                // https://github.com/prime31/Nez/blob/master/Nez.Portable/Utils/Collections/FastList.cs#L103
                Array.Copy(_components, i + 1, _components, i, _componentCount - i);
                _components[_componentCount] = null;

                GC.Collect();
                
                Console.WriteLine(_components.Length);
                
                return;
            }
        }

        throw new CubicException("Given component does not exist in the entity.");
    }

    private struct ComponentState
    {
        public Type Component;
        public object[] Args;
        public bool Add;

        public ComponentState(Type component, bool add, object[] args)
        {
            Component = component;
            Add = add;
            Args = args;
        }
    }
}