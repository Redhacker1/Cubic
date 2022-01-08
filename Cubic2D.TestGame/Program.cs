// See https://aka.ms/new-console-template for more information

using Cubic2D.Scenes;
using Cubic2D.Windowing;
using Cubic2D.TestGame;

GameSettings settings = new GameSettings();

SceneManager.RegisterScene<TestScene>("hello");

using (CubicGame game = new CubicGame(settings))
    game.Run();