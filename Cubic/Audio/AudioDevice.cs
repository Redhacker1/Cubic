using System;
using System.Numerics;
using System.Threading;
using OpenTK.Audio.OpenAL;

namespace Cubic.Audio;

public sealed class AudioDevice : IDisposable
{
    public event OnBufferFinished BufferFinished;
    
    private readonly ALDevice _device;
    private readonly ALContext _context;
    
    private readonly int[] _sources;
    private readonly (bool persist, bool loop)[] _channels;

    private float _masterVolume;

    // When a tracker track is playing, sounds will not be able to automatically play on the channels it allocates,
    // unless manually told to play on those channels
    internal int TrackChannels;

    /// <summary>
    /// The master volume for this <see cref="AudioDevice"/>. A value of 1.0 is "full volume".
    /// </summary>
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = value;
            AL.Listener(ALListenerf.Gain, value);
        }
    }

    /// <summary>
    /// The number of channels this <see cref="AudioDevice"/> has. This value represents the maximum number of
    /// <see cref="Sound"/>s that can be played at once.
    /// </summary>
    public readonly int NumChannels;

    private int _channelCount;
    
    internal unsafe AudioDevice(int numChannels)
    {
        _device = ALC.OpenDevice(null);
        _context = ALC.CreateContext(_device, (int*) null);
        ALC.MakeContextCurrent(_context);
        _sources = new int[numChannels];
        _channels = new (bool persist, bool loop)[numChannels];
        AL.GenSources(numChannels, _sources);
        MasterVolume = 1;
        NumChannels = numChannels;
        _channelCount = -1;
    }

    public Vector3 CameraPosition
    {
        set => AL.Listener(ALListener3f.Position, value.X, value.Y, value.Z);
    }

    public Quaternion CameraOrientation
    {
        set
        {
            OpenTK.Mathematics.Quaternion quat = new OpenTK.Mathematics.Quaternion(value.X, value.Y, value.Z, value.W);
            OpenTK.Mathematics.Vector3 direction = OpenTK.Mathematics.Vector3.Transform(OpenTK.Mathematics.Vector3.UnitZ, quat);
            OpenTK.Mathematics.Vector3 up = OpenTK.Mathematics.Vector3.Transform(OpenTK.Mathematics.Vector3.UnitY, quat);
            AL.Listener(ALListenerfv.Orientation, ref direction, ref up);
        }
    }

    /// <summary>
    /// Play the given sound on the given channel. If any sound is playing on this channel, it will be overriden by the
    /// new sound. <b>This includes tracker songs.</b>
    /// </summary>
    /// <param name="channel">The channel the sound will play on.</param>
    /// <param name="sound">The sound to play.</param>
    /// <param name="pitch">The pitch the sound should play at.</param>
    /// <param name="volume">The volume the sound should play at.</param>
    /// <param name="persistent">If persistent is enabled, the sound cannot be overriden by <see cref="PlayBuffer(Cubic.Audio.Sound,float,float,bool)"/> even if it runs out of channels.</param>
    public void PlayBuffer(int channel, AudioBuffer buffer, float pitch = 1, float volume = 1, bool loop = false, bool persistent = false, Vector3 position = default, bool relative = false)
    {
        _channels[channel].persist = persistent;
        int source = _sources[channel];
        AL.SourceStop(source);
        AL.Source(source, ALSourcei.Buffer, 0);
        AL.SourceQueueBuffer(source, buffer.Handle);
        AL.Source(source, ALSourcef.Pitch, pitch);
        AL.Source(source, ALSourcef.Gain, volume);
        AL.Source(source, ALSourceb.Looping, loop);
        AL.Source(source, ALSource3f.Position, position.X, position.Y, position.Z);
        AL.Source(source, ALSourceb.SourceRelative, relative);
        AL.SourcePlay(source);
    }

    /// <summary>
    /// Play the given sound on the next available channel.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="pitch">The pitch the sound should play at.</param>
    /// <param name="volume">The volume the sound should play at.</param>
    /// <param name="persistent">If persistent is enabled, the sound will not be overriden even if the number of available channels runs out.</param>
    /// <returns>The channel this sound is playing on.</returns>
    public int PlayBuffer(AudioBuffer buffer, float pitch = 1, float volume = 1, bool loop = false, bool persistent = false, Vector3 position = default, bool relative = false)
    {
        IncrementChannelCount(TrackChannels, NumChannels);
        PlayBuffer(_channelCount, buffer, pitch, volume, loop, persistent, position, relative);
        return _channelCount;
    }

    /// <summary>
    /// Play the given sound on the next available channel, within the bounds of the <paramref name="minChannel"/> and
    /// <paramref name="maxChannel"/>. The channel the sound is played on will not be outside of this boundary.
    /// <paramref name="minChannel"/> <b>is</b> inclusive, however <paramref name="maxChannel"/> is <b>not</b> inclusive.
    /// </summary>
    /// <param name="minChannel">The minimum channel this sound can play on.</param>
    /// <param name="maxChannel">The maximum channel this sound can play on.</param>
    /// <param name="sound">The sound to play.</param>
    /// <param name="pitch">The pitch the sound should play at.</param>
    /// <param name="volume">The volume the sound should play at.</param>
    /// <param name="persistent">If persistent is enabled, the sound will not be overriden even if the number of available channels runs out.</param>
    /// <returns>The channel this sound is playing on.</returns>
    public int PlayBuffer(int minChannel, int maxChannel, AudioBuffer buffer, float pitch = 1, float volume = 1,
        bool loop = false, bool persistent = false)
    {
        IncrementChannelCount(minChannel, maxChannel);
        PlayBuffer(_channelCount, buffer, pitch, volume, loop, persistent);
        return _channelCount;
    }

    /// <summary>
    /// Queue the sound onto the given channel. It will play once the first sound has finished.
    /// Useful for creating tracks with an intro and a loop. (Alternatively, you can set the sound's loop points.)<br />
    /// Must be queued onto a playing channel, otherwise it will not play.
    /// </summary>
    /// <param name="channel">The channel to queue the sound.</param>
    /// <param name="sound">The sound itself.</param>
    public void QueueBuffer(int channel, AudioBuffer buffer, bool loop = false)
    {
        int source = _sources[channel];
        AL.SourceQueueBuffer(source, buffer.Handle);
        _channels[channel].loop = loop;
    }

    private void IncrementChannelCount(int minChannel, int maxChannel)
    {
        int numIterations = 0;
        do
        {
            // Increment channel count by 1, looping back round to 0 if we exceed the max number of channels.
            // This approach will look for a free channel without any sound effects playing in it, which is what will
            // happen 99% of the time.
            _channelCount++;
            if (_channelCount >= maxChannel)
                _channelCount = minChannel;
            numIterations++;
            
            // If no free slot can be found, however, we take a more forceful approach.
            // This approach will overwrite even playing sounds with the new sound effect.
            // However, "persistent" sounds won't be overwritten, as the user has told the sound device this sound
            // should not be overwritten regardless. Therefore, if there are too many persistent sounds, an exception
            // will be thrown as the sounds cannot be overwritten. In 99% of situation there will be only 1 or 2
            // persistent sounds so this shouldn't be a problem.
            if (numIterations >= maxChannel - minChannel)
            {
                numIterations = 0;
                do
                {
                    _channelCount++;
                    numIterations++;
                    if (_channelCount >= maxChannel)
                        _channelCount = minChannel;
                    if (numIterations >= maxChannel - minChannel)
                        throw new Exception("Too many persistent sounds, new sound effect cannot be created.");
                } while (_channels[_channelCount].persist);

                break;
            }
        } while (IsPlaying(_channelCount));
    }

    /// <summary>
    /// Check if the given channel has a sound playing.
    /// </summary>
    /// <param name="channel">The channel to check.</param>
    /// <returns>True, if the channel has a sound playing.</returns>
    public bool IsPlaying(int channel)
    {
        // If the sound effect is playing, return true.
        // This also disables any persistent sounds that are no longer playing, as it frees up channels for the audio
        // device to use.
        bool playing = false;
        if (AL.GetSourceState(_sources[channel]) == ALSourceState.Playing)
            playing = true;
        else if (_channels[channel].persist)
            _channels[channel].persist = false;

        return playing;
    }

    /// <summary>
    /// Change the given sound's properties. Note: This will overwrite <b>ALL</b> properties on the sound's channel.
    /// </summary>
    /// <param name="channel">The sound's channel.</param>
    /// <param name="pitch">The pitch the sound should play at.</param>
    /// <param name="volume">The volume the sound should play at.</param>
    /// <param name="loop">Should the sound loop?</param>
    /// <param name="persistent">If persistent is enabled, the sound cannot be overriden by <see cref="PlayBuffer(Cubic.Audio.Sound,float,float,bool)"/> even if it runs out of channels.</param>
    public void SetSoundProperties(int channel, float pitch = 1, float volume = 1, bool loop = false,
        bool persistent = false)
    {
        int source = _sources[channel];
        AL.Source(source, ALSourcef.Pitch, pitch);
        AL.Source(source, ALSourcef.Gain, volume);
        AL.Source(source, ALSourceb.Looping, loop);
        _channels[channel].persist = persistent;
    }

    /// <summary>
    /// Stop the sound on the current channel from playing. This will also disable its persistence, if enabled.
    /// </summary>
    /// <param name="channel">The channel the sound is playing on.</param>
    public void Stop(int channel)
    {
        // As the sound effect is no longer playing we set its persistence to false.
        _channels[channel].persist = false;
        AL.SourceStop(_sources[channel]);
    }

    /// <summary>
    /// Pause the sound on the given channel. You can resume it with <see cref="Resume"/>.
    /// </summary>
    /// <param name="channel">The channel the sound is playing on.</param>
    public void Pause(int channel)
    {
        AL.SourcePause(_sources[channel]);
    }

    /// <summary>
    /// Resume the paused sound on the given channel.
    /// </summary>
    /// <param name="channel">The channel the sound is playing on.</param>
    public void Resume(int channel)
    {
        AL.SourcePlay(_sources[channel]);
    }

    /// <summary>
    /// Set the pitch of the sound in the given channel, without affecting the other parameters.
    /// </summary>
    /// <param name="channel">The channel that should be affected.</param>
    /// <param name="pitch">The pitch.</param>
    public void SetPitch(int channel, float pitch)
    {
        AL.Source(_sources[channel], ALSourcef.Pitch, pitch);
    }
    
    /// <summary>
    /// Set the volume of the sound in the given channel, without affecting the other parameters.
    /// </summary>
    /// <param name="channel">The channel that should be affected.</param>
    /// <param name="volume">The volume.</param>
    public void SetVolume(int channel, float volume)
    {
        AL.Source(_sources[channel], ALSourcef.Gain, volume);
    }
    
    /// <summary>
    /// Set whether the sound in the given channel should loop, without affecting the other parameters.
    /// </summary>
    /// <param name="channel">The channel that should be affected.</param>
    /// <param name="loop">Loop?</param>
    public void SetLooping(int channel, bool loop)
    {
        AL.Source(_sources[channel], ALSourceb.Looping, loop);
    }

    /// <summary>
    /// Set whether the sound in the given channel is persistent, without affecting the other parameters.
    /// </summary>
    /// <param name="channel">The channel that should be affected.</param>
    /// <param name="persistent">Persistent?</param>
    public void SetPersistent(int channel, bool persistent)
    {
        _channels[channel].persist = persistent;
    }

    public AudioBuffer CreateBuffer()
    {
        return new AudioBuffer(AL.GenBuffer());
    }

    public void UpdateBuffer<T>(AudioBuffer buffer, AudioFormat format, T[] data, int sampleFrequency) where T : unmanaged
    {
        ALFormat alFormat = format switch
        {
            AudioFormat.Mono8 => ALFormat.Mono8,
            AudioFormat.Mono16 => ALFormat.Mono16,
            AudioFormat.Stereo8 => ALFormat.Stereo8,
            AudioFormat.Stereo16 => ALFormat.Stereo16,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
        
        AL.BufferData(buffer.Handle, alFormat, data, sampleFrequency);
    }

    internal void Update()
    {
        // I hate the fact that I have to do this cause in theory if the frame rate is slow enough it could screw
        // up this whole thing, but in practice it shouldn't be a massive deal, it just needs to run at some point
        // for the queue to be cleared, before it loops
        for (int i = 0; i < NumChannels; i++)
        {
            int source = _sources[i];
            AL.GetSource(source, ALGetSourcei.BuffersProcessed, out int buffProcessed);

            if (buffProcessed > 0)
            {
                AL.SourceUnqueueBuffers(_sources[i], buffProcessed);
                BufferFinished?.Invoke(i);
                AL.GetSource(source, ALGetSourcei.BuffersQueued, out int buffQueued);
                if (buffQueued <= 1)
                    AL.Source(_sources[i], ALSourceb.Looping, _channels[i].loop);
            }
        }
    }

    public void Dispose()
    {
        AL.SourceStop(NumChannels, _sources);
        AL.DeleteSources(NumChannels, ref _sources[0]);

        ALC.MakeContextCurrent(ALContext.Null);
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
    }

    public delegate void OnBufferFinished(int channel);
}

public enum AudioFormat
{
    Mono8,
    Mono16,
    Stereo8,
    Stereo16
}