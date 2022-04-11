using Cubic.Render;

namespace Cubic.Primitives;

public interface IPrimitive
{
    public VertexPositionTextureNormal[] Vertices { get; }
    
    public uint[] Indices { get; }
}