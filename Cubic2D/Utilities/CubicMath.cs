using System;

namespace Cubic2D.Utilities;

public static class CubicMath
{
    public static float ToRadians(float degrees) => degrees * (MathF.PI / 180);

    public static float ToDegrees(float radians) => radians * (180 / MathF.PI);
}