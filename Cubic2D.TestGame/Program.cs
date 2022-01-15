using System.Drawing;
using Cubic2D.Scenes;
using Cubic2D.Windowing;
using Cubic2D.TestGame;

GameSettings settings = new GameSettings()
{
    Resizable = true
};

SceneManager.RegisterScene<TestScene>("hello");

using (CubicGame game = new CubicGame(settings))
    game.Run();