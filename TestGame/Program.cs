// See https://aka.ms/new-console-template for more information

using Cubic2D.Windowing;

GameSettings settings = new GameSettings();

using (CubicGame game = new CubicGame(settings))
    game.Run();