using System;
using Cubic2D.Render;
using Cubic2D.Scenes;

namespace Cubic2D.TestGame;

public class TestScene : Scene
{
    protected override void Initialize()
    {
        base.Initialize();
        
        Console.WriteLine("Initialized!");
    }

    protected override void Update()
    {
        base.Update();
    }
}