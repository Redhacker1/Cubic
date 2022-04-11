using System.Numerics;
using Cubic.Utilities;

namespace Cubic.Entities;

public class Camera2D : Entity
{
    public Matrix4x4 TransformMatrix { get; private set; }

    internal void GenerateTransformMatrix()
    {
        // TODO: Add rotation and scale too.
        TransformMatrix = Matrix4x4.CreateTranslation(new Vector3(-Transform.Position.ToVector2(), 0));
    }
    
    public static Camera2D Main { get; internal set; }
}