using System;
using static Cubic.Audio.AudioDevice;

namespace Cubic.Audio;

public struct AudioBuffer : IDisposable
{
    internal uint Handle;

    public readonly bool Exists;

    internal AudioBuffer(uint handle)
    {
        Handle = handle;
        Exists = true;
    }
    
    public void Dispose()
    {
        Al.DeleteBuffer(Handle);
    }
}