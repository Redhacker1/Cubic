using System.Numerics;
using Cubic.Utilities;

namespace Cubic.Entities;

public class Camera2D : Entity
{
    public bool UseCustomTransformMatrix;

    public Matrix4x4 TransformMatrix;

    public Camera2D()
    {
        UseCustomTransformMatrix = false;
    }

    internal void GenerateTransformMatrix()
    {
        if (UseCustomTransformMatrix)
            return;
        // TODO: Add rotation and scale too.
        TransformMatrix = Matrix4x4.CreateTranslation(new Vector3(-Transform.Position.ToVector2(), 0));
    }
    
    public static Camera2D Main { get; internal set; }
}