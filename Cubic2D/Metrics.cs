namespace Cubic2D;

public static class Metrics
{
    private static int _fps;
    private static int _framesInTick;
    private static ulong _totalFrames;
    private static float _lastTick;
    
    internal static ulong SpritesDrawnInternal;
    internal static ulong DrawCallsInternal;

    /// <summary>
    /// The total number of calls issued to the current SpriteRenderer during the last frame.
    /// </summary>
    public static ulong SpritesDrawn => SpritesDrawnInternal;
    
    /// <summary>
    /// The total number of calls issued to the GPU during the last frame.
    /// </summary>
    public static ulong DrawCalls => DrawCallsInternal;
    
    /// <summary>
    /// The total number of frames that have passed since the application opened.
    /// </summary>
    public static ulong TotalFrames => _totalFrames;

    /// <summary>
    /// The current FPS (Frames per Second) of the application. This value updates once per second.
    /// </summary>
    public static int Fps => _fps;

    internal static void Update()
    {
        SpritesDrawnInternal = 0;
        DrawCallsInternal = 0;
        
        _totalFrames++;
        _framesInTick++;

        float time = (float) Time.Stopwatch.Elapsed.TotalSeconds;
        if (time - _lastTick >= 1d)
        {
            _lastTick = time;
            _fps = _framesInTick;
            _framesInTick = 0;
        }
    }

    public static string GetString()
    {
        return
            $"METRICS:\nFPS: {Fps}\nFrames: {TotalFrames}\nSprites: {SpritesDrawn}\nDraw calls: {DrawCalls}";
    }
}