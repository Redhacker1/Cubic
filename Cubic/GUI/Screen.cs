using Cubic.Render;
using Cubic.Scenes;
using Cubic.Windowing;

namespace Cubic.GUI;

public abstract class Screen
{
    protected internal CubicGame Game { get; internal set; }
    protected Graphics Graphics => Game.GraphicsInternal;

    protected Scene CurrentScene => SceneManager.Active;
    
    protected internal virtual void Initialize() { }

    protected internal virtual void Open() { }

    protected internal virtual void Update() { }

    protected internal virtual void Draw() { }

    protected internal virtual void Close()
    {
        SceneManager.Active.CloseScreenInternal();
    }
}