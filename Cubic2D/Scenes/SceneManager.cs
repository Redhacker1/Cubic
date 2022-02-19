using System;
using System.Collections.Generic;
using System.Linq;
using Cubic2D.Windowing;

namespace Cubic2D.Scenes;

public static class SceneManager
{
    private static readonly Dictionary<string, Type> _scenes = new Dictionary<string, Type>();

    /// <summary>
    /// Get the currently active scene.
    /// </summary>
    public static Scene ActiveScene => Active;
    
    internal static Scene Active;
    private static Type _switchScene;

    internal static void Initialize(CubicGame game)
    {
        if (_scenes.Count < 1)
            throw new CubicException("There must be at least one scene registered before the application can launch.");

        Type scene = _scenes.ElementAt(0).Value;
        Active = (Scene) Activator.CreateInstance(scene);
        if (Active == null)
            throw new CubicException("Scene could not be instantiated.");
        Active.Game = game;
        Active.Graphics = game.Graphics;
        Active.Initialize();
    }
    
    internal static void Update(CubicGame game)
    {
        if (_switchScene != null)
        {
            Active.Dispose();
            Active = null;
            // Force the GC to collect our now null scene, as we don't need it.
            GC.Collect();
            Active = (Scene) Activator.CreateInstance(_switchScene);
            _switchScene = null;
            if (Active == null)
                throw new CubicException("Scene could not be instantiated.");
            Active.Game = game;
            Active.Graphics = game.Graphics;
            Active.Initialize();
        }
        
        Active.Update();
    }

    internal static void Draw()
    {
        Active.DrawInternal();
    }

    /// <summary>
    /// Register a scene so it can be used.
    /// </summary>
    /// <param name="sceneType">The scene's type. It <b>must</b> derive off <see cref="Scene"/>.</param>
    /// <param name="name">The name of the scene.</param>
    public static void RegisterScene(Type sceneType, string name)
    {
        if (sceneType.BaseType == null || sceneType.BaseType != typeof(Scene))
            throw new CubicException($"Given scene must derive off {typeof(Scene)}");
        _scenes.Add(name, sceneType);
    }

    /// <summary>
    /// Set the 
    /// </summary>
    /// <param name="name"></param>
    public static void SetScene(string name) => _switchScene = _scenes[name];
}