using System;
using System.IO;
using Cubic2D.Scenes;
using OpenTK.Audio.OpenAL;

namespace Cubic2D.Audio;

/// <summary>
/// Represents a sound sample that can be played by an <see cref="AudioDevice"/>.
/// </summary>
public partial struct Sound : IDisposable
{
    public byte[] Data;
    public readonly int Channels;
    public readonly int SampleRate;
    public readonly int BitsPerSample;

    internal readonly ALFormat Format;
    internal readonly int Buffer;
    internal readonly int LoopBuffer;

    public readonly bool Loop;
    public int BeginLoopPoint;
    public int EndLoopPoint;

    public Sound(byte[] data, int channels, int sampleRate, int bitsPerSample, bool loop = false, int beginLoopPoint = 0, int endLoopPoint = -1)
    {
        Data = data;
        Channels = channels;
        SampleRate = sampleRate;
        BitsPerSample = bitsPerSample;
        Loop = loop;
        BeginLoopPoint = beginLoopPoint;
        EndLoopPoint = endLoopPoint == -1 ? Data.Length : endLoopPoint;
        Buffer = 0;
        LoopBuffer = -1;
        Format = ALFormat.Mono8;
        Format = GetFormat(channels, bitsPerSample);
        CreateBuffers(out Buffer, out LoopBuffer);
        SceneManager.Active.CreatedResources.Add(this);
    }

    /// <summary>
    /// Create a new <see cref="Sound"/> from the given path.<br />
    /// Accepts the following file types:
    /// <list type="bullet">
    ///     <item>.wav</item>
    /// </list>
    /// </summary>
    /// <param name="path">The path to the sound file.</param>
    /// <param name="loop">Does this sound loop?</param>
    /// <param name="beginLoopPoint">The sample number the loop starts at.</param>
    /// <param name="endLoopPoint">The sample number the loop ends at. Set to -1 for the loop point to be placed at the end of the sound.</param>
    /// <exception cref="Exception">Thrown if the given file is not an accepted file type, or if the given file is invalid/corrupt.</exception>
    /// <remarks><paramref name="beginLoopPoint"/> and <paramref name="endLoopPoint"/> are only used if <paramref name="loop"/> is set.</remarks>
    public Sound(string path, bool loop = false, int beginLoopPoint = 0, int endLoopPoint = -1)
    {
        string ext = Path.GetExtension(path).ToLower();
        Data = ext switch
        {
            ".wav" => LoadWav(File.OpenRead(path), out Channels, out SampleRate, out BitsPerSample),
            ".ctra" => LoadCtra(File.OpenRead(path), out Channels, out SampleRate, out BitsPerSample, out beginLoopPoint, out endLoopPoint),
            ".ogg" => LoadOgg(File.ReadAllBytes(path), out Channels, out SampleRate, out BitsPerSample),
            _ => throw new Exception("Given file is not a valid type.")
        };

        Loop = loop;
        // sampleToBytes calculates the correct multiplier to convert the given sample number in begin and endLoopPoint,
        // into the correct byte multiplier for Data.
        // This ensures that no matter the format of the data, the loop points will always be consistent.
        int sampleToBytes = (Channels * BitsPerSample) / 8;
        BeginLoopPoint = beginLoopPoint * sampleToBytes;
        EndLoopPoint = endLoopPoint == -1 ? Data.Length : endLoopPoint * sampleToBytes;
        
        Buffer = 0;
        LoopBuffer = -1;
        Format = ALFormat.Mono8;
        Format = GetFormat(Channels, BitsPerSample);
        CreateBuffers(out Buffer, out LoopBuffer);
        
        SceneManager.Active.CreatedResources.Add(this);
    }

    private ALFormat GetFormat(int channels, int bits)
    {
        return channels switch
        {
            1 => bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16,
            2 => bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16,
            _ => throw new Exception("Not valid file")
        };
    }

    private void CreateBuffers(out int buffer1, out int buffer2)
    {
        /*const int newSampleRate = 11025;
        // Calculate our alignment for the number of bytes we need.
        int alignment = (Channels * BitsPerSample) / 8;
        // This will help us determine (a) How many bytes our new data array should be, (b) the section of data we copy
        // across to this new array from the old array for any given value of i.
        float ratio = SampleRate / (float) newSampleRate;
        byte[] data = new byte[(int) ((Data.Length * ratio) - (Data.Length * ratio) % alignment)];
        for (int i = 0; i < data.Length; i += alignment)
        {
            // dataPoint calculates the exact starting array index for any given value of i, based on our computed
            // ratio. It is aligned to the correct byte, so dataPoint + alignment will always be the next sample in the
            // array.
            int dataPoint = (int) ((i * 1 / ratio) - (i * 1 / ratio) % alignment);
            // Append the data for the alignment we need. Typically, alignment will be 4.
            // (16 bits per sample * 2 channels) / sizeof(byte) = 4. The first two bytes will be the first channel and
            // the last two bytes will be the second channel.
            if (dataPoint >= Data.Length)
                break;
            for (int a = 0; a < alignment; a++)
                data[i + a] = Data[dataPoint + a];
        }

        Data = data;*/
        
        buffer1 = AL.GenBuffer();
        buffer2 = -1;
        if (Loop)
        {
            //BeginLoopPoint = (int) ((BeginLoopPoint * ratio) - (BeginLoopPoint * ratio) % alignment);
            //EndLoopPoint = (int) ((EndLoopPoint * ratio) - (EndLoopPoint * ratio) % alignment);
            if (BeginLoopPoint > 0)
            {
                AL.BufferData(buffer1, Format, Data[..BeginLoopPoint], SampleRate);
                buffer2 = AL.GenBuffer();
                AL.BufferData(buffer2, Format, Data[BeginLoopPoint..EndLoopPoint], SampleRate);
            }
            else
                AL.BufferData(buffer1, Format, Data[..EndLoopPoint], SampleRate);
        }
        else
            AL.BufferData(buffer1, Format, Data, SampleRate);
    }

    public void Dispose()
    {
        AL.DeleteBuffer(Buffer);
        if (AL.IsBuffer(LoopBuffer))
            AL.DeleteBuffer(LoopBuffer);
        
#if DEBUG
        Console.WriteLine("Sound disposed.");
#endif
    }
}