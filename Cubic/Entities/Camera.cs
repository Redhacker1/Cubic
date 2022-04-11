using System;
using System.Drawing;
using System.Numerics;
using Cubic.Utilities;

namespace Cubic.Entities;

public class Camera : Entity
{
    private float _fov;
    private float _near;
    private float _far;

    public float Fov
    {
        get => CubicMath.ToDegrees(_fov);
        set
        {
            _fov = CubicMath.ToRadians(value);
            GenerateProjectionMatrix();
        }
    }

    public float NearPlane
    {
        get => _near;
        set
        {
            _near = value;
            GenerateProjectionMatrix();
        }
    }

    public float FarPlane
    {
        get => _far;
        set
        {
            _far = value;
            GenerateProjectionMatrix();
        }
    }
    
    public Matrix4x4 ProjectionMatrix { get; private set; }
    
    public Matrix4x4 ViewMatrix { get; private set; }

    public Camera()
    {
        // 45deg
        _fov = MathF.PI / 4;
        _near = 0.1f;
        _far = 1000f;
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        GenerateProjectionMatrix();
        Game.GraphicsInternal.ViewportResized += ViewportResized;
    }

    internal void GenerateViewMatrix()
    {
        ViewMatrix = Matrix4x4.CreateLookAt(Transform.Position, Transform.Position + Transform.Forward, Transform.Up);
    }

    internal void GenerateProjectionMatrix()
    {
        Size winSize = Game.GraphicsInternal.Viewport.Size;
        ProjectionMatrix =
            Matrix4x4.CreatePerspectiveFieldOfView(_fov, winSize.Width / (float) winSize.Height, _near, _far);
    }
    
    private void ViewportResized(Size size)
    {
        GenerateProjectionMatrix();
    }
    
    public static Camera Main { get; internal set; }
}