namespace Cubic2D;

public static class Metrics
{
    internal static ulong SpritesDrawnInternal;
    internal static ulong DrawCallsInternal;

    public static ulong SpritesDrawn => SpritesDrawnInternal;

    public static ulong DrawCalls => DrawCallsInternal;

    internal static void Reset()
    {
        SpritesDrawnInternal = 0;
        DrawCallsInternal = 0;
    }
}