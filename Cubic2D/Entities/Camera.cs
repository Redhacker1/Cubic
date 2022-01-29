using System;
using System.Numerics;

namespace Cubic2D.Entities;

public class Camera : Entity
{
    public Matrix4x4 TransformMatrix { get; private set; }

    internal void GenerateTransformMatrix()
    {
        // TODO: Add rotation and scale too.
        TransformMatrix = Matrix4x4.CreateTranslation(new Vector3(-Transform.Position, 0));
    }
    
    public static Camera Main { get; internal set; }
}