using System;
using System.IO;
using OpenTK.Audio.OpenAL;

namespace Cubic2D.Audio;

/// <summary>
/// Represents a sound sample that can be played by an <see cref="AudioDevice"/>.
/// </summary>
public struct Sound
{
    public readonly byte[] Data;
    public readonly int Channels;
    public readonly int SampleRate;
    public readonly int BitsPerSample;

    internal readonly ALFormat Format;

    public Sound(byte[] data, int channels, int sampleRate, int bitsPerSample)
    {
        Data = data;
        Channels = channels;
        SampleRate = sampleRate;
        BitsPerSample = bitsPerSample;
        Format = ALFormat.Mono8;
        GetFormat(channels, bitsPerSample);
    }

    /// <summary>
    /// Create a new <see cref="Sound"/> from the given path.<br />
    /// Accepts the following file types:
    /// <list type="bullet">
    ///     <item>.wav</item>
    /// </list>
    /// </summary>
    /// <param name="path">The path to the sound file.</param>
    /// <exception cref="Exception">Thrown if the given file is not an accepted file type, or if the given file is invalid/corrupt.</exception>
    public Sound(string path)
    {
        string ext = Path.GetExtension(path);
        switch (ext)
        {
            case ".wav":
                Data = LoadWav(File.OpenRead(path), out Channels, out SampleRate, out BitsPerSample);
                break;
            default:
                throw new Exception("Given file is not a valid type.");
        }

        Format = ALFormat.Mono8;
        Format = GetFormat(Channels, BitsPerSample);
    }

    public static byte[] LoadWav(Stream stream, out int channels, out int sampleRate, out int bitsPerSample)
    {
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Header
            
            if (new string(reader.ReadChars(4)) != "RIFF") // ChunkID
                throw new Exception("Given file is not a wave file.");
            
            reader.ReadInt32(); // ChunkSize
            
            if (new string(reader.ReadChars(4)) != "WAVE") // Format
                throw new Exception("Given wave file is not valid.");
            
            if (new string(reader.ReadChars(4)) != "fmt ") // Subchunk1ID
                throw new Exception("Given wave file is not valid.");

            reader.ReadInt32(); // Subchunk1Size
            
            if (reader.ReadInt16() != 1) // AudioFormat
                throw new Exception("Compressed wave files cannot be loaded.");

            channels = reader.ReadInt16();
            sampleRate = reader.ReadInt32();

            reader.ReadInt32(); // ByteRate, we just calculate this when needed.
            reader.ReadInt16(); // BlockAlign

            bitsPerSample = reader.ReadInt16();
            
            // Data
            
            if (new string(reader.ReadChars(4)) != "data") // Subchunk2ID
                throw new Exception("Given wave file is not valid.");

            int size = reader.ReadInt32(); // Subchunk2Size
            return reader.ReadBytes(size);
        }
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
}