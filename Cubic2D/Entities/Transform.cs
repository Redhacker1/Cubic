using System.Numerics;

namespace Cubic2D.Entities;

public class Transform
{
    public Vector2 Position;
    public Vector2 Scale;
    public Vector2 Origin;
    public float Rotation;

    public Transform()
    {
        Position = Vector2.Zero;
        Scale = Vector2.One;
        Origin = Vector2.Zero;
        Rotation = 0;
    }
}