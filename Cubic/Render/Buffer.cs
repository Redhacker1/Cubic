using OpenTK.Graphics.OpenGL4;

namespace Cubic.Render;

public class Buffer
{
    internal int Handle;
    internal BufferTarget Target;

    internal Buffer(int handle, BufferTarget target)
    {
        Handle = handle;
        Target = target;
    }
}