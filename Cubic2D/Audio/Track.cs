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

public struct Track
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

    private byte[] _orders;

    private byte _currentRow;
    private byte _currentPattern;

    internal Track(Sample[] samples, Pattern[] patterns, byte[] orders, byte initialTempo, byte initialSpeed)
    {
        _title = "";
        _author = "";

        _samples = samples;
        _patterns = patterns;
        _orders = orders;

        Tempo = initialTempo;
        Speed = initialSpeed;

        _currentRow = 0;
        _currentPattern = 0;
        _trackVolume = 1;
    }

    internal static Track FromS3M(byte[] data)
    {
        using MemoryStream memStream = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(memStream);

        Console.WriteLine(new string(reader.ReadChars(28))); // title
        if (reader.ReadByte() != 0x1A) // sig1
            throw new CubicException("Invalid s3m");
        reader.ReadByte(); // type
        reader.ReadUInt16(); // reserved

        ushort horderCount = reader.ReadUInt16();
        ushort instrumentCount = reader.ReadUInt16();
        ushort patternPtrCount = reader.ReadUInt16();
        ushort hFlags = reader.ReadUInt16();
        reader.ReadUInt16(); // trackerVersion
        reader.ReadUInt16(); // sampleType - for now we'll just assume unsigned samples.

        if (new string(reader.ReadChars(4)) != "SCRM") // sig2
            throw new CubicException("Invalid s3m");

        byte globalVolume = reader.ReadByte();

        byte initialSpeed = reader.ReadByte();
        byte initialTempo = reader.ReadByte();
        byte masterVolume = reader.ReadByte();
        reader.ReadByte(); // ultraClickRemoval
        byte defaultPan = reader.ReadByte();
        reader.ReadBytes(8); // reserved
        reader.ReadUInt16(); // ptrSpecial - we'll just assume this is not set.

        byte[] channelSettings = reader.ReadBytes(32);
        byte[] orderList = reader.ReadBytes(horderCount);
        ushort[] ptrInstruments = new ushort[instrumentCount];
        for (int i = 0; i < instrumentCount; i++)
            ptrInstruments[i] = reader.ReadUInt16();

        ushort[] ptrPatterns = new ushort[patternPtrCount];
        for (int i = 0; i < patternPtrCount; i++)
            ptrPatterns[i] = reader.ReadUInt16();

        Sample[] samples = new Sample[instrumentCount];
        for (int i = 0; i < instrumentCount; i++)
        {
            reader.BaseStream.Position = ptrInstruments[i] * 16;
            //Console.WriteLine(reader.BaseStream.Position);
            if (reader.ReadByte() != 1)
                throw new CubicException("OPL instruments are not supported, sorry.");
            reader.ReadBytes(12);

            reader.ReadByte();
            // For some unknown reason they chose to use 24 bits!!
            uint pData = (uint) (reader.ReadByte() | reader.ReadByte() << 8);
            samples[i].Length = reader.ReadUInt32();
            samples[i].LoopBegin = reader.ReadUInt32();
            samples[i].LoopEnd = reader.ReadUInt32();
            samples[i].Volume = reader.ReadByte();
            reader.ReadBytes(2); // pack, not used
            samples[i].Flags = reader.ReadByte();
            samples[i].SampleRate = reader.ReadUInt32();
            reader.ReadBytes(12); // Unused data & stuff we don't need. No soundblaster or GUS here!
            Console.WriteLine(reader.ReadChars(28)); // We also don't need sample name either.
            if (new string(reader.ReadChars(4)) != "SCRS")
                throw new CubicException("Instrument header has not been read correctly.");
            reader.BaseStream.Position = pData * 16;
            //Console.WriteLine(reader.BaseStream.Position);
            samples[i].Data = reader.ReadBytes((int) samples[i].Length);
        }

        //dev.PlaySound(new Sound(samples[0].Data, 2, (int) samples[0].SampleRate, 4, true));

        Pattern[] patterns = new Pattern[patternPtrCount];
        for (int i = 0; i < patternPtrCount; i++)
        {
            reader.BaseStream.Position = ptrPatterns[i] * 16;
            Console.WriteLine(reader.BaseStream.Position);
            reader.ReadUInt16();
            patterns[i] = new Pattern(32);
            for (int r = 0; r < patterns[i].Length; r++)
            {
                byte flag = reader.ReadByte();
                if (flag == 0)
                    continue;

                byte channel = (byte) (flag & 31);
                byte note = 255;
                byte instrument = 0;
                byte volume = 64;
                byte specialCommand = 255;
                byte commandInfo = 0;
                
                if ((flag & 32) == 32)
                {
                    note = reader.ReadByte();
                    instrument = reader.ReadByte();
                }

                if ((flag & 64) == 64)
                    volume = reader.ReadByte();

                if ((flag & 128) == 128)
                {
                    specialCommand = reader.ReadByte();
                    commandInfo = reader.ReadByte();
                }

                PianoKey key = PianoKey.None;
                if (note == 254)
                    key = PianoKey.NoteCut;
                else
                    key = (PianoKey) (note & 0xF) + 2;

                Octave octave = (Octave) (note >> 4);

                //Console.WriteLine(
                //    $"Row: {r}, Channel: {channel}, Key: {key}, Octave: {octave}, Instrument: {instrument}, Volume: {volume}");

                patterns[i].SetNote(r, channel, new Note(key, octave, instrument, volume));
            }
        }

        return new Track(samples, patterns, orderList, initialTempo, initialSpeed);
    }

    internal byte[] ToPCM(byte channels, uint sampleRate, byte bitsPerSample, out int beginLoopPoint,
        out int endLoopPoint)
    {
        int tempo = Tempo;
        int speed = Speed;

        Channel[] chn = new Channel[32];
        
        int rowDurationInMs = (2500 / tempo) * speed;

        byte[] data = Array.Empty<byte>();
        for (int p = 0; p < _orders.Length; p++)
        {
            Pattern pattern = _patterns[_orders[p]];
            Array.Resize(ref data,
                data.Length + (int) ((sampleRate * rowDurationInMs) / 1000) * (bitsPerSample / 8) * channels);
            for (int c = 0; c < chn.Length; c++)
            {
                Note n = pattern.Notes[c, _currentRow];
                if (!n.Initialized)
                    continue;

                if (n.Key != PianoKey.None)
                {
                    PitchNote pn = new PitchNote(n.Key, n.Octave, n.Volume);
                    chn[c].SampleRate = (uint) (_samples[n.SampleNum].SampleRate * pn.Pitch);
                    chn[c].Volume = pn.Volume;
                    chn[c].Ratio = sampleRate / (float) chn[c].SampleRate;
                    chn[c].SamplePos = 0;
                }
            }
            for (int i = 0; i < (sampleRate * rowDurationInMs) / 1000; i++)
            {
                for (int c = 0; c < chn.Length; c++)
                {
                    
                }
            }
        }
    }

    internal struct Sample
    {
        public uint Length;
        public uint LoopBegin;
        public uint LoopEnd;
        public byte Volume;
        public byte Flags;
        public uint SampleRate;
        public byte[] Data;
    }

    private struct Channel
    {
        public float Ratio;
        public float Volume;
        public uint SampleRate;
        public uint SamplePos;
        public byte SampleID;
    }
}