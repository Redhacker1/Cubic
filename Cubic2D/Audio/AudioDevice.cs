using System;
using Cubic2D.Render;
using OpenTK.Audio.OpenAL;

namespace Cubic2D.Audio;

public sealed class AudioDevice : IDisposable
{
    private readonly ALDevice _device;
    private readonly ALContext _context;
    
    private readonly int[] _sources;
    private readonly bool[] _persistentSources;

    private float _masterVolume;

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
        _persistentSources = new bool[numChannels];
        AL.GenSources(numChannels, _sources);
        MasterVolume = 1;
        NumChannels = numChannels;
        _channelCount = -1;
    }

    /// <summary>
    /// Play the given sound on the given channel. If any sound is playing on this channel, it will be overriden by the
    /// new sound.
    /// </summary>
    /// <param name="channel">The channel the sound will play on.</param>
    /// <param name="sound">The sound to play.</param>
    /// <param name="pitch">The pitch the sound should play at.</param>
    /// <param name="volume">The volume the sound should play at.</param>
    /// <param name="persistent">If persistent is enabled, the sound cannot be overriden by <see cref="PlaySound(Cubic2D.Audio.Sound,float,float,bool)"/> even if it runs out of channels.</param>
    public void PlaySound(int channel, Sound sound, float pitch = 1, float volume = 1, bool persistent = false)
    {
        _persistentSources[channel] = persistent;
        int source = _sources[channel];
        AL.SourceStop(source);
        AL.Source(source, ALSourcei.Buffer, 0);
        AL.SourceQueueBuffer(source, sound.Buffer);
        if (sound.Loop && sound.BeginLoopPoint > 0)
            AL.SourceQueueBuffer(source, sound.LoopBuffer);
        AL.Source(source, ALSourcef.Pitch, pitch);
        AL.Source(source, ALSourcef.Gain, volume);
        AL.Source(source, ALSourceb.Looping, sound.Loop && sound.BeginLoopPoint == 0);
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
    public int PlaySound(Sound sound, float pitch = 1, float volume = 1, bool persistent = false)
    {
        IncrementChannelCount();
        PlaySound(_channelCount, sound, pitch, volume, persistent);
        return _channelCount;
    }

    /// <summary>
    /// Queue the sound onto the given channel. It will play once the first sound has finished.
    /// Useful for creating tracks with an intro and a loop. (Alternatively, you can set the sound's loop points.)<br />
    /// Must be queued onto a playing channel, otherwise it will not play.
    /// </summary>
    /// <param name="channel">The channel to queue the sound.</param>
    /// <param name="sound">The sound itself.</param>
    public void QueueSound(int channel, Sound sound)
    {
        int source = _sources[channel];
        AL.SourceQueueBuffer(source, sound.Buffer);
        if (sound.Loop && sound.BeginLoopPoint > 0)
            AL.SourceQueueBuffer(source, sound.LoopBuffer);
    }

    private void IncrementChannelCount()
    {
        int numIterations = 0;
        do
        {
            // Increment channel count by 1, looping back round to 0 if we exceed the max number of channels.
            // This approach will look for a free channel without any sound effects playing in it, which is what will
            // happen 99% of the time.
            _channelCount++;
            if (_channelCount >= NumChannels)
                _channelCount = 0;
            numIterations++;
            
            // If no free slot can be found, however, we take a more forceful approach.
            // This approach will overwrite even playing sounds with the new sound effect.
            // However, "persistent" sounds won't be overwritten, as the user has told the sound device this sound
            // should not be overwritten regardless. Therefore, if there are too many persistent sounds, an exception
            // will be thrown as the sounds cannot be overwritten. In 99% of situation there will be only 1 or 2
            // persistent sounds so this shouldn't be a problem.
            if (numIterations >= NumChannels)
            {
                numIterations = 0;
                do
                {
                    _channelCount++;
                    numIterations++;
                    if (_channelCount >= NumChannels)
                        _channelCount = 0;
                    if (numIterations >= NumChannels)
                        throw new Exception("Too many persistent sounds, new sound effect cannot be created.");
                } while (_persistentSources[_channelCount]);

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
        else if (_persistentSources[channel])
            _persistentSources[channel] = false;

        return playing;
    }

    /// <summary>
    /// Change the given sound's properties. Note: This will overwrite <b>ALL</b> properties on the sound's channel.
    /// </summary>
    /// <param name="channel">The sound's channel.</param>
    /// <param name="pitch">The pitch the sound should play at.</param>
    /// <param name="volume">The volume the sound should play at.</param>
    /// <param name="loop">Should the sound loop?</param>
    /// <param name="persistent">If persistent is enabled, the sound cannot be overriden by <see cref="PlaySound(Cubic2D.Audio.Sound,float,float,bool)"/> even if it runs out of channels.</param>
    public void SetSoundProperties(int channel, float pitch = 1, float volume = 1, bool loop = false,
        bool persistent = false)
    {
        int source = _sources[channel];
        AL.Source(source, ALSourcef.Pitch, pitch);
        AL.Source(source, ALSourcef.Gain, volume);
        AL.Source(source, ALSourceb.Looping, loop);
        _persistentSources[channel] = persistent;
    }

    /// <summary>
    /// Stop the sound on the current channel from playing. This will also disable its persistence, if enabled.
    /// </summary>
    /// <param name="channel">The channel the sound is playing on.</param>
    public void Stop(int channel)
    {
        // As the sound effect is no longer playing we set its persistence to false.
        _persistentSources[channel] = false;
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
        _persistentSources[channel] = persistent;
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
                AL.GetSource(source, ALGetSourcei.BuffersQueued, out int buffQueued);
                if (buffQueued <= 1)
                    AL.Source(_sources[i], ALSourceb.Looping, true);
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
}