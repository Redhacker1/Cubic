using System;
using System.Collections.Generic;

namespace Cubic2D.Scenes;

public static class SceneManager
{
    private static Dictionary<string, Type> _scenes;

    internal static Scene Active;
    private static Scene _switchScene;

    internal static void Update()
    {
        if (_switchScene != null)
        {
            
        }
        
        Active.Update();
    }

    public static void RegisterScene<T>(string name) where T : Scene
    {
        _scenes.Add(name, typeof(T));
    }
}