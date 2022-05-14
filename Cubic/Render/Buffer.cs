using Silk.NET.OpenGL;

namespace Cubic.Render;

public class Buffer
{
    internal int Handle;
    internal BufferTargetARB Target;

    internal Buffer(int handle, BufferTargetARB target)
    {
        Handle = handle;
        Target = target;
    }
}