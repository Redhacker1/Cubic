using System;

namespace Cubic2D.Utilities;

public static class CubicMath
{
    public static float ToRadians(float degrees) => degrees * (MathF.PI / 180);

    public static float ToDegrees(float radians) => radians * (180 / MathF.PI);

    public static float Clamp(float value, float min, float max) => value <= min ? min : value >= max ? max : value;

    public static float Lerp(float value1, float value2, float multiplier) => value1 + multiplier * (value2 - value1);
}