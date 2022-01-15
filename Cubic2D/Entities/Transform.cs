using System;
using System.Numerics;

namespace Cubic2D.Entities;

public class Transform
{
    /// <summary>
    /// The position of this transform.
    /// </summary>
    public Vector2 Position;
    
    /// <summary>
    /// The scale of this transform.
    /// </summary>
    public Vector2 Scale;
    
    /// <summary>
    /// The origin point of this transform.
    /// </summary>
    public Vector2 Origin;
    
    /// <summary>
    /// The rotation, in radians, of this transform.
    /// </summary>
    public float Rotation;

    /// <summary>
    /// The "Forward" vector of this transform. If <see cref="Rotation"/> is 0, <see cref="Forward"/> will be in the
    /// positive X direction.
    /// </summary>
    /// <remarks>This value is the same as <see cref="Right"/>.</remarks>
    public Vector2 Forward => new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));

    /// <summary>
    /// The "Backward" vector of this transform. If <see cref="Rotation"/> is 0, <see cref="Backward"/> will be in the
    /// negative X direction.
    /// </summary>
    /// <remarks>This value is the same as <see cref="Left"/>.</remarks>
    public Vector2 Backward => new Vector2(-MathF.Cos(Rotation), -MathF.Sin(Rotation));
    
    /// <summary>
    /// The "Right" vector of this transform. If <see cref="Rotation"/> is 0, <see cref="Right"/> will be in the
    /// positive X direction.
    /// </summary>
    /// <remarks>This value is the same as <see cref="Forward"/>.</remarks>
    public Vector2 Right => new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
    
    /// <summary>
    /// The "Left" vector of this transform. If <see cref="Rotation"/> is 0, <see cref="Left"/> will be in the
    /// negative X direction.
    /// </summary>
    /// <remarks>This value is the same as <see cref="Backward"/>.</remarks>
    public Vector2 Left => new Vector2(-MathF.Cos(Rotation), -MathF.Sin(Rotation));
    
    /// <summary>
    /// The "Up" vector of this transform. If <see cref="Rotation"/> is 0, <see cref="Up"/> will be in the
    /// positive Y direction.
    /// </summary>
    public Vector2 Up => new Vector2(MathF.Sin(Rotation), -MathF.Cos(Rotation));

    /// <summary>
    /// The "Down" vector of this transform. If <see cref="Rotation"/> is 0, <see cref="Down"/> will be in the
    /// negative Y direction.
    /// </summary>
    public Vector2 Down => new Vector2(-MathF.Sin(Rotation), MathF.Cos(Rotation));
    
    public Transform()
    {
        Position = Vector2.Zero;
        Scale = Vector2.One;
        Origin = Vector2.Zero;
        Rotation = 0;
    }
}