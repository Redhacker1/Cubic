using System;
using System.Diagnostics;

namespace Cubic2D;

public static class Time
{
    internal static Stopwatch Stopwatch;
    private static float _lastTime;
    private static float _deltaTime;
    private static float _lastTick;
    private static int _fps;
    private static int _framesInTick;
    private static ulong _totalFrames;

    internal static float LastTime;

    /// <summary>
    /// The amount of time the last frame took to process. Use to create framerate-independent movement.
    /// </summary>
    public static float DeltaTime => _deltaTime;

    /// <summary>
    /// The total number of frames that have passed since the application opened.
    /// </summary>
    public static ulong TotalFrames => _totalFrames;

    /// <summary>
    /// The current FPS (Frames per Second) of the application. This value updates once per second.
    /// </summary>
    public static int Fps => _fps;

    /// <summary>
    /// The total number of milliseconds that have passed since the application opened.
    /// </summary>
    public static long TotalMilliseconds => Stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// The total number of seconds that have passed since the application opened.
    /// </summary>
    public static float TotalSeconds => (float) Stopwatch.Elapsed.TotalSeconds;

    internal static void Start()
    {
        Stopwatch = Stopwatch.StartNew();
        _lastTime = 0;
        _deltaTime = 0;
    }

    internal static void Update()
    {
        float time = (float) Stopwatch.Elapsed.TotalSeconds;
        LastTime = time;
        _deltaTime = time - _lastTime;
        _lastTime = time;
        _totalFrames++;
        _framesInTick++;

        if (time - _lastTick >= 1d)
        {
            _lastTick = time;
            _fps = _framesInTick;
            _framesInTick = 0;
        }
    }
}