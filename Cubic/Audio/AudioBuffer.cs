using System;
using OpenTK.Audio.OpenAL;

namespace Cubic.Audio;

public struct AudioBuffer : IDisposable
{
    internal int Handle;

    public readonly bool Exists;

    internal AudioBuffer(int handle)
    {
        Handle = handle;
        Exists = true;
    }
    
    public void Dispose()
    {
        AL.DeleteBuffer(Handle);
    }
}