using System;
using System.IO;
using Cubic.Scenes;
using StbVorbisSharp;

namespace Cubic.Audio;

/// <summary>
/// Represents a sound sample that can be played by an <see cref="AudioDevice"/>.
/// </summary>
public partial class Sound : IDisposable
{
    public byte[] Data;
    public readonly int Channels;
    public readonly int SampleRate;
    public readonly int BitsPerSample;

    public readonly bool Loop;
    public int BeginLoopPoint;
    public int EndLoopPoint;

    private AudioBuffer[] _buffers;

    private AudioDevice _device;

    private SoundType _type;

    private Vorbis _vorbis;

    private int _activeChannel;
    private int _currentBuffer;

    private int _count;

    public Sound(AudioDevice device, byte[] data, int channels, int sampleRate, int bitsPerSample, bool loop = false, int beginLoopPoint = 0, int endLoopPoint = -1)
    {
        _device = device;
        Data = data;
        Channels = channels;
        SampleRate = sampleRate;
        BitsPerSample = bitsPerSample;
        Loop = loop;
        BeginLoopPoint = beginLoopPoint;
        EndLoopPoint = endLoopPoint == -1 ? Data.Length : endLoopPoint;

        _buffers = null;
        _type = SoundType.PCM;

        _vorbis = null;
        _activeChannel = -1;
        _currentBuffer = 0;
        Load();

        SceneManager.Active.CreatedResources.Add(this);
    }

    /// <summary>
    /// Create a new <see cref="Sound"/> from the given path.<br />
    /// Accepts the following file types:
    /// <list type="bullet">
    ///     <item>.wav</item>
    ///     <item>.ogg</item>
    ///     <item>.ctra</item>
    ///     <item>.s3m</item>
    /// </list>
    /// </summary>
    /// <param name="path">The path to the sound file.</param>
    /// <param name="loop">Does this sound loop?</param>
    /// <param name="beginLoopPoint">The sample number the loop starts at.</param>
    /// <param name="endLoopPoint">The sample number the loop ends at. Set to -1 for the loop point to be placed at the end of the sound.</param>
    /// <param name="interpolation">If true, linear interpolation will be used. This option is <b>only</b> valid for modules.</param>
    /// <exception cref="Exception">Thrown if the given file is not an accepted file type, or if the given file is invalid/corrupt.</exception>
    /// <remarks><paramref name="beginLoopPoint"/> and <paramref name="endLoopPoint"/> are only used if <paramref name="loop"/> is set.</remarks>
    public Sound(AudioDevice device, string path, bool loop = false, int beginLoopPoint = 0, int endLoopPoint = -1, bool interpolation = true)
    {
        _device = device;
        int soundLoopPoint = 0;

        _vorbis = null;

        string ext = Path.GetExtension(path).ToLower();
        switch (ext)
        {
            case ".wav":
                Data = LoadWav(File.ReadAllBytes(path), out Channels, out SampleRate, out BitsPerSample);
                _type = SoundType.PCM;
                break;
            case ".ctra":
                Data = LoadCtra(File.OpenRead(path), out Channels, out SampleRate, out BitsPerSample,
                    out soundLoopPoint, out endLoopPoint);
                _type = SoundType.Track;
                break;
            case ".ogg":
                _vorbis = Vorbis.FromMemory(File.ReadAllBytes(path));
                Data = null;
                Channels = _vorbis.Channels;
                SampleRate = _vorbis.SampleRate;
                BitsPerSample = 16;
                _type = SoundType.Vorbis;
                break;
            case ".s3m":
                Data = null;
                Channels = 2;
                SampleRate = 44100;
                BitsPerSample = 2;
                _type = SoundType.Track;
                LoadS3M(File.ReadAllBytes(path));
                break;
            case ".it":
                Data = null;
                Channels = 2;
                SampleRate = 44100;
                BitsPerSample = 2;
                _type = SoundType.Track;
                LoadIT(File.ReadAllBytes(path));
                break;
            default:
                    throw new Exception("Given file is not a valid type.");
        }

        if (beginLoopPoint == 0 && soundLoopPoint != 0)
            beginLoopPoint = soundLoopPoint;

        Loop = loop;
        // sampleToBytes calculates the correct multiplier to convert the given sample number in begin and endLoopPoint,
        // into the correct byte multiplier for Data.
        // This ensures that no matter the format of the data, the loop points will always be consistent.
        int sampleToBytes = (Channels * BitsPerSample) / 8;
        BeginLoopPoint = beginLoopPoint * sampleToBytes;
        if (Data == null)
            EndLoopPoint = 0;
        else
            EndLoopPoint = endLoopPoint == -1 ? Data.Length : endLoopPoint * sampleToBytes;

        _buffers = null;
        _activeChannel = -1;
        _currentBuffer = 0;

        _interpolation = interpolation;

        Load();
        
        _device.BufferFinished += DeviceOnBufferFinished;

        SceneManager.Active.CreatedResources.Add(this);
    }

    public static AudioFormat GetFormat(int channels, int bits)
    {
        return channels switch
        {
            1 => bits == 8 ? AudioFormat.Mono8 : AudioFormat.Mono16,
            2 => bits == 8 ? AudioFormat.Stereo8 : AudioFormat.Stereo16,
            _ => throw new Exception("Unsupported audio format provided.")
        };
    }

    private void Load()
    {
        switch (_type)
        {
            case SoundType.PCM:
                _buffers = new AudioBuffer[2];
                AudioFormat format = GetFormat(Channels, BitsPerSample);
                _buffers[0] = _device.CreateBuffer();
                if (Loop)
                {
                    if (BeginLoopPoint > 0)
                    {
                        _device.UpdateBuffer(_buffers[0], format, Data[..BeginLoopPoint], SampleRate);
                        _buffers[1] = _device.CreateBuffer();
                        _device.UpdateBuffer(_buffers[1], format, Data[BeginLoopPoint..EndLoopPoint], SampleRate);
                    }
                    else
                        _device.UpdateBuffer(_buffers[0], format, Data[..EndLoopPoint], SampleRate);
                }
                else
                    _device.UpdateBuffer(_buffers[0], format, Data, SampleRate);

                break;
            case SoundType.Vorbis:
                _buffers = new AudioBuffer[2];
                for (int i = 0; i < _buffers.Length; i++)
                {
                    _buffers[i] = _device.CreateBuffer();
                    GetVorbisData();
                    IncrementBuffer();
                }
                break;
            case SoundType.Track:
                _trackBuffer = new short[SampleRate];
                _buffers = new AudioBuffer[2];
                for (int i = 0; i < _buffers.Length; i++)
                {
                    _buffers[i] = _device.CreateBuffer();
                    GetTrackData();
                    IncrementBuffer();
                }

                break;
        }
    }
    
    private void DeviceOnBufferFinished(int channel)
    {
        if (channel != _activeChannel || _type == SoundType.PCM)
            return;

        switch (_type)
        {
            case SoundType.Vorbis:
                GetVorbisData();
                break;
            case SoundType.Track:
                GetTrackData();
                break;
        }
        
        _device.QueueBuffer(_activeChannel, _buffers[_currentBuffer]);
        IncrementBuffer();
    }

    private void GetVorbisData()
    {
        _vorbis.SubmitBuffer();

        if (_vorbis.Decoded < _vorbis.SampleRate / 2)
        {
            //StbVorbis.stb_vorbis_seek(_vorbis.StbVorbis, );
            _vorbis.Restart();
            _vorbis.SubmitBuffer();
        }

        short[] data = _vorbis.SongBuffer;
        _device.UpdateBuffer(_buffers[_currentBuffer], AudioFormat.Stereo16, data, _vorbis.SampleRate);
    }

    private void IncrementBuffer()
    {
        _currentBuffer++;
        if (_currentBuffer >= _buffers.Length)
            _currentBuffer = 0;
    }

    public int Play(int channel = -1, float pitch = 1, float volume = 1, bool persistent = false)
    {
        switch (_type)
        {
            case SoundType.PCM:
                if (_buffers[1].Exists)
                {
                    if (channel == -1)
                        channel = _device.PlayBuffer(_buffers[0], pitch, volume, persistent: persistent);
                    else
                        _device.PlayBuffer(channel, _buffers[0], pitch, volume, persistent: persistent);
                    _device.QueueBuffer(channel, _buffers[1], Loop);
                }
                else if (channel == -1)
                    channel = _device.PlayBuffer(_buffers[0], pitch, volume, persistent: persistent, loop: Loop);
                else
                    _device.PlayBuffer(channel, _buffers[0], pitch, volume, persistent: persistent, loop: Loop);

                break;
            default:
                if (channel == -1)
                    channel = _device.PlayBuffer(_buffers[0], pitch, volume, persistent: persistent);
                else
                    _device.PlayBuffer(channel, _buffers[0], pitch, volume, persistent: persistent);
                
                for (int i = 1; i < _buffers.Length; i++)
                    _device.QueueBuffer(channel, _buffers[i]);
                break;
        }
        
        _activeChannel = channel;
        return channel;
    }

    public void Dispose()
    {
        foreach (AudioBuffer buffer in _buffers)
            if (buffer.Exists)
                buffer.Dispose();
        
        _vorbis?.Dispose();

        _device.BufferFinished -= DeviceOnBufferFinished;
        
#if DEBUG
        Console.WriteLine("Sound disposed.");
#endif
    }
}

public enum SoundType
{
    PCM,
    Vorbis,
    Track
}