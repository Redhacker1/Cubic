using Cubic2D.Render;
using Cubic2D.Windowing;

namespace Cubic2D.Utilities;

public abstract class GlobalScript
{
    public bool Enabled;

    internal GlobalScript()
    {
        Enabled = true;
    }
    
    protected internal CubicGame Game { get; internal set; }

    protected internal virtual void Initialize() { }

    protected internal void Update() { }

    protected internal virtual void Draw(Graphics graphics) { }
}