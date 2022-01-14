using System.Diagnostics;

namespace Cubic2D;

public static class Time
{
    private static Stopwatch _sw;
    private static double _lastTime;
    private static double _deltaTime;

    /// <summary>
    /// The amount of time the last frame took to process. Use to create framerate-independent movement.
    /// </summary>
    public static float DeltaTime => (float) _deltaTime;

    internal static void Start()
    {
        _sw = Stopwatch.StartNew();
        _lastTime = 0;
        _deltaTime = 0;
    }

    internal static void Update()
    {
        double time = _sw.Elapsed.TotalSeconds;
        _deltaTime = time - _lastTime;
        _lastTime = time;
    }
}