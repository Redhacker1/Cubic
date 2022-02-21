using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Timers;
using Cubic2D.Scenes;
using OpenTK.Audio.OpenAL;
using Timer = System.Timers.Timer;

namespace Cubic2D.Audio;

public struct Track : IDisposable
{
    private string _title;
    private string _author;

    /// <summary>
    /// This track's title, if any. Limited to 25 characters.
    /// </summary>
    public string Title => _title;

    /// <summary>
    /// This track's author, if any. Limited to 25 characters.
    /// </summary>
    public string Author => _author;

    /// <summary>
    /// The tempo of this track, in bpm.
    /// </summary>
    public readonly int Tempo;
    
    /// <summary>
    /// The speed of this track, in ticks per row.
    /// </summary>
    public readonly int Speed;

    private float _trackVolume;

    private Pattern[] _patterns;

    private Sample[] _samples;

    private Channel[] _channels;

    private AudioDevice _device;

    private byte[] _alignmentBuffer;

    private int[] _buffers;

    private byte _currentRow;
    private byte _currentPattern;

    public Track(AudioDevice device, string path, float trackVolume)
    {
        _device = device;
        _trackVolume = trackVolume;
        _alignmentBuffer = new byte[4];
        
        Console.WriteLine("CTRA->Sound converter");
        for (int i = 0; i < 25; i++)
            Console.Write('-');
        Console.WriteLine();
        using DeflateStream deflateStream = new DeflateStream(File.OpenRead(path), CompressionMode.Decompress);
        using BinaryReader reader = new BinaryReader(deflateStream);
        if (new string(reader.ReadChars(10)) != "CUBICTRACK")
            throw new CubicException("Given CTRA is not a cubic track.");
        reader.ReadUInt32();
        _title = reader.ReadChars(25).ToString();
        _author = reader.ReadChars(25).ToString();

        Tempo = reader.ReadByte();
        Console.WriteLine("Tempo: " + Tempo);
        Speed = reader.ReadByte();
        Console.WriteLine("Speed: " + Speed);

        if (new string(reader.ReadChars(7)) != "SAMPLES")
            throw new CubicException("Given CTRA file is not formed correctly (corrupted?).");
        byte numSamples = reader.ReadByte();
        Console.WriteLine("NumSamples: " + numSamples);
        _samples = new Sample[numSamples];
        for (int sample = 0; sample < numSamples; sample++)
        {
            Console.Write("Loading Sample " + sample + "... ");
            reader.ReadByte();
            _samples[sample].SampleRate = reader.ReadUInt32();
            _samples[sample].BitsPerSample = reader.ReadByte();
            _samples[sample].Channels = reader.ReadByte();
            bool loop = reader.ReadBoolean();
            _samples[sample].Loop = loop;
            if (loop)
            {
                _samples[sample].BeginLoopPoint = reader.ReadUInt32();
                _samples[sample].EndLoopPoint = reader.ReadUInt32();
            }

            uint dataLength = reader.ReadUInt32();
            _samples[sample].Data = reader.ReadBytes((int) dataLength);
            _samples[sample].Alignment = (byte) ((_samples[sample].Channels * _samples[sample].BitsPerSample) / 8);
            Console.WriteLine("Done");
        }
        
        if (new string(reader.ReadChars(8)) != "PATTERNS")
            throw new CubicException("Given CTRA file is not formed correctly (corrupted?).");

        byte numPatterns = reader.ReadByte();
        _patterns = new Pattern[numPatterns];
        Console.WriteLine("NumPatterns: " + numPatterns);
        for (int pattern = 0; pattern < numPatterns; pattern++)
        {
            Console.Write("Loading Pattern " + pattern + "... ");
            reader.ReadByte();
            byte pLength = reader.ReadByte();
            byte pChannels = reader.ReadByte();
            _patterns[pattern] = new Pattern(pChannels, pLength);
            for (int channel = 0; channel < pChannels; channel++)
            {
                for (int row = 0; row < pLength; row++)
                {
                    if (!reader.ReadBoolean())
                        continue;
                    _patterns[pattern].SetNote(row, channel,
                        new Note((PianoKey) reader.ReadByte(), (Octave) reader.ReadByte(), reader.ReadByte(),
                            reader.ReadByte(), (Effect) reader.ReadByte(), reader.ReadByte()));
                }
            }
            
            Console.WriteLine("Done");
        }

        int maxChannels = 0;
        for (int i = 0; i < _patterns.Length; i++)
        {
            if (_patterns[i].NumChannels > maxChannels)
                maxChannels = _patterns[i].NumChannels;
        }

        _channels = new Channel[maxChannels];

        _buffers = AL.GenBuffers(_channels.Length);

        _currentPattern = 0;
        _currentRow = 0;

        SceneManager.Active.CreatedResources.Add(this);
    }


    //private byte[] GetSampleAtPoint(uint samplePoint)
    //{
        
        
    //}
    
    private struct Sample
    {
        public byte[] Data;
        public uint SampleRate;
        public byte BitsPerSample;
        public byte Channels;
        public byte Alignment;
        public bool Loop;
        public uint BeginLoopPoint;
        public uint EndLoopPoint;
    }

    private struct Channel
    {
        public uint SamplePos;
        public byte SampleID;
    }

    public void Dispose()
    {
        AL.DeleteBuffers(_channels.Length, _buffers);
    }
}