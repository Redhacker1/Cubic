using System.Numerics;

namespace Cubic.Render;

public struct VertexPosition
{
    public Vector3 Position;

    public VertexPosition(Vector3 position)
    {
        Position = position;
    }
}