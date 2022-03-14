using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Timers;
using Cubic2D.Scenes;
using Cubic2D.Utilities;
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

    private Track(Sample[] samples, Pattern[] patterns, byte[] orders, byte initialTempo, byte initialSpeed)
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
            byte type = reader.ReadByte();
            if (type == 0)
                continue;
            if (type != 1)
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
            byte flags = reader.ReadByte();
            if ((flags & 1) == 1)
                samples[i].Loop = true;
            if ((flags & 2) == 2) { } // for now ignore
            samples[i].Type = (flags & 4) == 4 ? SampleType.SixteenBit : SampleType.EightBit;
            
            //samples[i].Flags = reader.ReadByte();
            samples[i].SampleRate = reader.ReadUInt32();
            reader.ReadBytes(12); // Unused data & stuff we don't need. No soundblaster or GUS here!
            reader.ReadChars(28); // We also don't need sample name either.
            if (new string(reader.ReadChars(4)) != "SCRS")
                throw new CubicException("Instrument header has not been read correctly.");
            reader.BaseStream.Position = pData * 16;
            samples[i].Data = reader.ReadBytes((int) samples[i].Length);
            UnsignedToSigned(samples[i].Type, ref samples[i].Data);
            samples[i].Type = SampleType.SixteenBit;
        }

        //dev.PlaySound(new Sound(samples[0].Data, 2, (int) samples[0].SampleRate, 4, true));

        Pattern[] patterns = new Pattern[patternPtrCount];
        for (int i = 0; i < patternPtrCount; i++)
        {
            reader.BaseStream.Position = ptrPatterns[i] * 16;
            reader.ReadUInt16();
            patterns[i] = new Pattern(32);
            for (int r = 0; r < patterns[i].Length; r++)
            {
                byte flag;
                do
                {
                    flag = reader.ReadByte();
                    if (flag == 0)
                        break;

                    byte channel = (byte) (flag & 31);
                    byte note = 255;
                    byte instrument = 0;
                    byte volume = 64;
                    byte specialCommand = 255;
                    byte commandInfo = 0;

                    if ((flag & 32) == 32)
                    {
                        note = reader.ReadByte();
                        instrument = (byte) (reader.ReadByte() - 1);
                    }

                    if ((flag & 64) == 64)
                        volume = reader.ReadByte();

                    if ((flag & 128) == 128)
                    {
                        specialCommand = reader.ReadByte();
                        commandInfo = reader.ReadByte();
                    }
                    

                    PianoKey key = PianoKey.None;
                    Octave octave = Octave.Octave0;
                    if (note != 255)
                    {
                        if (note == 254)
                            key = PianoKey.NoteCut;
                        else
                            key = (PianoKey) (note & 0xF) + 2;
                        
                        octave = (Octave) (note >> 4);
                    }

                    Effect effect = Effect.None;
                    switch (specialCommand)
                    {
                        case 1:
                            effect = Effect.SetSpeed;
                            break;
                        case 3:
                            effect = Effect.PatternBreak;
                            break;
                    }

                    Console.WriteLine(
                        $"Pattern: {i}, Row: {r}, Channel: {channel}, Key: {key}, Octave: {octave}, Instrument: {instrument}, Volume: {volume}, SpecialCMD: {specialCommand}, SpecialParam: {commandInfo}");

                    patterns[i].SetNote(r, channel, new Note(key, octave, instrument, volume, effect, commandInfo));
                } while (flag != 0);
            }
        }

        return new Track(samples, patterns, orderList, initialTempo, initialSpeed);
    }

    internal byte[] ToPCM(byte channels, uint sampleRate, byte bitsPerSample, out int beginLoopPoint,
        out int endLoopPoint)
    {
        int tempo = Tempo;
        int speed = Speed;
        int dataPos = 0;
        
        Channel[] chn = new Channel[32];
        
        int rowDurationInMs = (2500 / tempo) * speed;

        byte[] data = Array.Empty<byte>();
        for (int p = 0; p < _orders.Length; p++)
        {
            if (_orders[p] == 255)
                continue;

            bool shouldIncreasePattern = false;
            Pattern pattern = _patterns[_orders[p]];
            for (int r = 0; r < pattern.Length; r++)
            {
                for (int c = 0; c < chn.Length; c++)
                {
                    Note n = pattern.Notes[c, r];
                    if (!n.Initialized)
                        continue;

                    if (n.Key == PianoKey.NoteCut)
                    {
                        chn[c].SampleRate = 0;
                        chn[c].Ratio = 0;
                        chn[c].Volume = 0;
                        continue;
                    }
                    if (n.Key != PianoKey.None)
                    {
                        PitchNote pn = new PitchNote(n.Key, n.Octave, n.Volume);
                        chn[c].SampleID = (byte) n.SampleNum;
                        chn[c].SampleRate = (uint) (_samples[chn[c].SampleID].SampleRate * pn.Pitch);
                        chn[c].Volume = pn.Volume;
                        chn[c].Ratio = sampleRate / (float) chn[c].SampleRate;
                        chn[c].SamplePos = 0;
                    }
                    else
                    {
                        chn[c].Volume = n.Volume * PitchNote.RefVolume;
                    }

                    switch (n.Effect)
                    {
                        case Effect.PatternBreak:
                            shouldIncreasePattern = true;
                            break;
                        case Effect.SetSpeed:
                            speed = n.EffectParam;
                            rowDurationInMs = (2500 / tempo) * speed;
                            break;
                    }
                }
                
                Array.Resize(ref data,
                    data.Length + (int) ((sampleRate * rowDurationInMs) / 1000) * (bitsPerSample / 8) * channels);

                for (int i = 0; i < (sampleRate * rowDurationInMs) / 1000; i++)
                {
                    for (int c = 0; c < chn.Length; c++)
                    {
                        if (chn[c].Volume == 0)
                            continue;
                        chn[c].SamplePos += (1 / chn[c].Ratio) * 2;
                        if (_samples[chn[c].SampleID].Loop && chn[c].SamplePos >= _samples[chn[c].SampleID].LoopEnd * 2)
                        {
                            chn[c].SamplePos = _samples[chn[c].SampleID].LoopBegin * 2;
                        }

                        if (chn[c].SamplePos + 4 < _samples[chn[c].SampleID].Data.Length)
                        {
                            for (int a = 0; a < 4; a += 2)
                            {
                                short sample = (short) (data[dataPos + a] | data[dataPos + 1 + a] << 8);
                                short newSample = (short) (_samples[chn[c].SampleID].Data[(int) chn[c].SamplePos - (int) chn[c].SamplePos % 2 + a] |
                                                           _samples[chn[c].SampleID].Data[(int) chn[c].SamplePos - (int) chn[c].SamplePos % 2 + 1 + a] << 8);

                                short finalSample = (short) (sample + (newSample / 8) * chn[c].Volume);

                                finalSample = finalSample >= short.MaxValue ? short.MaxValue :
                                    finalSample <= short.MinValue ? short.MinValue : finalSample;

                                data[dataPos + a] = (byte) (finalSample & 0xFF);
                                data[dataPos + 1 + a] = (byte) (finalSample >> 8);
                            }
                        }
                    }

                    dataPos += 4;
                }

                if (shouldIncreasePattern)
                    break;
            }
        }

        beginLoopPoint = 0;
        endLoopPoint = -1;
        return data;
    }

    private static void UnsignedToSigned(SampleType type, ref byte[] data)
    {
        switch (type)
        {
            case SampleType.EightBit:
                byte[] dat = data;
                Array.Resize(ref data, data.Length << 1);
                for (int i = 0; i < dat.Length; i++)
                {
                    ushort sample = (ushort) (dat[i] << 8);
                    short signedSample = (short) (sample - short.MaxValue);
                    data[i * 2] = (byte) (signedSample & 0xFF);
                    data[i * 2 + 1] = (byte) (signedSample >> 8);
                }
                break;
            case SampleType.SixteenBit:
                for (int i = 0; i < data.Length - 1; i += 2)
                {
                    ushort sample = (ushort) (data[i] | data[i + 1] << 8);
                    short signedSample = (short) (sample - short.MaxValue);
                    data[i] = (byte) (signedSample & 0xFF);
                    data[i + 1] = (byte) (signedSample >> 8);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private struct Sample
    {
        public uint Length;
        public bool Loop;
        public uint LoopBegin;
        public uint LoopEnd;
        public byte Volume;
        public uint SampleRate;
        public byte[] Data;
        public SampleType Type;
    }

    private enum SampleType
    {
        EightBit,
        SixteenBit
    }

    private struct Channel
    {
        public float Ratio;
        public float Volume;
        public uint SampleRate;
        public float SamplePos;
        public byte SampleID;
        public Effect Effect;
    }
}