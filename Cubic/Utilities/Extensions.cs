using System;
using System.Drawing;
using System.Numerics;

namespace Cubic.Utilities;

public static class Extensions
{
    internal static Vector4 Normalize(this Color color)
    {
        return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    public static Vector2 ToVector2(this Size size)
    {
        return new Vector2(size.Width, size.Height);
    }

    public static Vector2 ToVector2(this Point point)
    {
        return new Vector2(point.X, point.Y);
    }

    public static Vector2 ToVector2(this Vector3 vector3)
    {
        return new Vector2(vector3.X, vector3.Y);
    }

    public static Vector3 ToVector3(this Vector4 vector4)
    {
        return new Vector3(vector4.X, vector4.Y, vector4.Z);
    }

    public static Point ToPoint(this Vector2 vector2)
    {
        return new Point((int) vector2.X, (int) vector2.Y);
    }

    public static Size ToSize(this Vector2 vector2)
    {
        return new Size((int) vector2.X, (int) vector2.Y);
    }

    public static Vector3 ToEulerAngles(this Quaternion quat)
    {
        // Convert our values to euler angles.
        // https://math.stackexchange.com/questions/2975109/how-to-convert-euler-angles-to-quaternions-and-get-the-same-euler-angles-back-fr

        float yaw = MathF.Asin(CubicMath.Clamp(2f * (quat.W * quat.Y - quat.Z * quat.X), -1f, 1f));
        
        float pitch = MathF.Atan2(2f * (quat.W * quat.X + quat.Y * quat.Z),
            1f - 2f * (quat.X * quat.X + quat.Y * quat.Y));

        float roll = MathF.Atan2(2f * (quat.W * quat.Z + quat.X * quat.Y),
            1f - 2f * (quat.Y * quat.Y + quat.Z * quat.Z));

        return new Vector3(yaw, pitch, roll);
    }

    public static Matrix4x4 To3x3Matrix(this Matrix4x4 matrix)
    {
        return new Matrix4x4(
            matrix.M11, matrix.M12, matrix.M13, 0,
            matrix.M21, matrix.M22, matrix.M23, 0,
            matrix.M31, matrix.M32, matrix.M33, 0,
            0,          0,          0,          1
        );
    }
}