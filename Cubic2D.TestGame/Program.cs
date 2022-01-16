using System;
using System.Drawing;
using Cubic2D.Render;
using Cubic2D.Scenes;
using Cubic2D.Windowing;
using Cubic2D.TestGame;

GameSettings settings = new GameSettings()
{
    Resizable = true,
    Api = GraphicsApi.Default
    //StartVisible = false,
};

SceneManager.RegisterScene<TestScene>("hello");

using (CubicGame game = new CubicGame(settings))
    game.Run();