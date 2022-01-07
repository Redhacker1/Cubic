using System.Drawing;

namespace Cubic2D.Windowing;

public struct GameSettings
{
    public Size Size;

    public string Title;

    public bool Fullscreen;

    public bool VSync;

    public GameSettings()
    {
        Size = new Size(1280, 720);
        Title = "Cubic2D Window";
        Fullscreen = false;
        VSync = true;
    }
}