namespace Cubic2D.Render;

public abstract class UnmanagedResource
{
    // I would use an interface for this but I can't make the methods internal.
    // Maybe I should allow manual disposal of unmanaged resources, but for now it will be entirely
    // dealt with by the engine.
    // TODO: Think about this
    internal abstract void Dispose();
}