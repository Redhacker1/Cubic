using System;
using System.IO;
using OpenTK.Audio.OpenAL;

namespace Cubic.Audio;

public struct Track
{
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

    private const int NumBuffers = 3;
    private const int BufferLengthInSamples = 44100 * 5;

    private byte[] _audioBuffer;
    private int[] _buffers;
    private int _currentBuffer;
    private AudioDevice _audioDevice;
    private int _activeChannel;

    private Track(AudioDevice device, Sample[] samples, Pattern[] patterns, byte[] orders, byte initialTempo,
        byte initialSpeed)
    {
        _samples = samples;
        _patterns = patterns;
        _orders = orders;

        Tempo = initialTempo;
        Speed = initialSpeed;

        _currentRow = 0;
        _currentPattern = 0;
        _trackVolume = 1;

        _currentBuffer = 0;
        _activeChannel = 0;

        _audioDevice = device;
        if (device != null)
        {
            _buffers = AL.GenBuffers(NumBuffers);
            _audioBuffer = new byte[BufferLengthInSamples];
            _activeChannel = -1;
            device.BufferFinished += DeviceOnBufferFinished;
            for (int i = 0; i < NumBuffers; i++)
            {
                FillBuffer();
                AL.BufferData(_buffers[i], ALFormat.Stereo16, _audioBuffer, 44100);
            }
        }
        else
        {
            _buffers = null;
            _audioBuffer = null;
        }
    }

    private void DeviceOnBufferFinished(int channel)
    {
        if (channel == _activeChannel)
        {
            FillBuffer();
            AL.BufferData(_buffers[_currentBuffer], ALFormat.Stereo16, _audioBuffer, 44100);
            _currentBuffer++;
            if (_currentBuffer >= NumBuffers)
                _currentBuffer = 0;
        }
    }

    public static Track Load(AudioDevice device, string path)
    {
        return FromS3M(device, File.ReadAllBytes(path));
    }

    internal static Track FromS3M(AudioDevice device, byte[] data)
    {
        // Load the S3M.
        // The supposedly official reference doc used extensively for this:
        // https://github.com/lclevy/unmo3/blob/master/spec/s3m.txt
        // .. As well as the OpenMPT source for reading the samples
        // https://github.com/OpenMPT/openmpt
        
        using MemoryStream memStream = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(memStream);

        reader.ReadChars(28); // title
        if (reader.ReadByte() != 0x1A) // sig1
            throw new CubicException("Invalid s3m");
        reader.ReadByte(); // type
        reader.ReadUInt16(); // reserved

        ushort orderCount = reader.ReadUInt16();
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
        reader.ReadByte(); // ultraClickRemoval - not needed, no GUS here.
        byte defaultPan = reader.ReadByte();
        reader.ReadBytes(8); // reserved
        reader.ReadUInt16(); // ptrSpecial - we'll just assume this is not set.

        byte[] channelSettings = reader.ReadBytes(32);
        byte[] orderList = reader.ReadBytes(orderCount);
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

            // I have no idea why this works, I just got this from the OpenMPT source code:
            // https://github.com/OpenMPT/openmpt/blob/master/soundlib/S3MTools.cpp#L141
            byte[] dataPointer = reader.ReadBytes(3);
            uint pData = (uint) ((dataPointer[1] << 4) | (dataPointer[2] << 12) | (dataPointer[0] << 20));
            samples[i].Length = reader.ReadUInt32();
            samples[i].LoopBegin = reader.ReadUInt32();
            samples[i].LoopEnd = reader.ReadUInt32();
            samples[i].Volume = reader.ReadByte();
            reader.ReadBytes(2); // pack, not used
            byte flags = reader.ReadByte();
            samples[i].Loop = (flags & 1) != 0;
            samples[i].Stereo = (flags & 2) != 0;
            samples[i].Type = (flags & 4) != 0 ? SampleType.SixteenBit : SampleType.EightBit;
            
            samples[i].SampleRate = reader.ReadUInt32();
            reader.ReadBytes(12); // Unused data & stuff we don't need. No soundblaster or GUS here!
            reader.ReadChars(28); // We also don't need sample name either.
            if (new string(reader.ReadChars(4)) != "SCRS")
                throw new CubicException("Instrument header has not been read correctly.");
            reader.BaseStream.Position = pData;
            // A stereo sample will use two bytes per sample for 8-bit, and four bytes per sample for 16-bit.
            // Think of the data arrays as looking like such:
            // [255, 38, 147, 0, ....] - 16 bit stereo sound
            // These 4 bytes represent one "sample". That means with a sampling rate of 44.1khz there will be 4 * 44100
            // of these for each second of audio. The first two bytes represent the left channel, and the last two represent
            // the right channel. To combine each set of bytes into a single value we can do:
            // short sample = data[i] | data[i] << 8
            // The data is little endian, so the second byte needs to go up top. (I've not met a PCM format with big endian yet..)
            // S3M uses signed samples however, so during playback all we need to do is use a ushort for our sample instead,
            // then subtract short.MaxValue from it to get the signed sample the resampler expects!
            samples[i].Data = reader.ReadBytes(!samples[i].Stereo ? (int) samples[i].Length : (int) samples[i].Length * 2);
        }

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

                    // ALl this data is stored in ONE byte! Mad!
                    // We do a bitwise AND of 31 here as the max number of channels supported is 32 (although the S3M
                    // spec only allows 16 PCM channels..)
                    byte channel = (byte) (flag & 31);
                    byte note = 255;
                    byte instrument = 0;
                    byte volume = 64;
                    byte specialCommand = 255;
                    byte commandInfo = 0;

                    // Check each of our flags here to set each value.
                    // To learn more, read the S3M format doc I linked further up.
                    if ((flag & 32) != 0)
                    {
                        note = reader.ReadByte();
                        instrument = (byte) (reader.ReadByte() - 1);
                    }

                    if ((flag & 64) != 0)
                        volume = reader.ReadByte();

                    if ((flag & 128) != 0)
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
                        case 6:
                            effect = Effect.PortamentoUp;
                            break;
                        case 20:
                            effect = Effect.SetTempo;
                            break;
                    }

                    //Console.WriteLine(
                    //    $"Pattern: {i}, Row: {r}, Channel: {channel}, Key: {key}, Octave: {octave}, Instrument: {instrument}, Volume: {volume}, SpecialCMD: {specialCommand}, SpecialParam: {commandInfo}");

                    patterns[i].SetNote(r, channel, new Note(key, octave, instrument, volume, effect, commandInfo));
                } while (flag != 0);
            }
        }

        return new Track(device, samples, patterns, orderList, initialTempo, initialSpeed);
    }

    internal byte[] ToPCM(byte channels, uint sampleRate, byte bitsPerSample, out int beginLoopPoint,
        out int endLoopPoint)
    {
        int tempo = Tempo;
        int speed = Speed;
        int dataPos = 0;
        
        // TODO: Change to allow arbitrary number of channels.
        Channel[] chn = new Channel[32];
        
        // For more info: https://wiki.openmpt.org/Manual:_Song_Properties#Overview
        // (Classic tempo is being used here)
        int rowDurationInMs = (2500 / tempo) * speed;
        int tickDurationInMs = (2500 / tempo);

        // Really I need to change this to be not a random value but if I change this it crashes :(
        // Leaving it alone for safety. Lol!
        // TODO: Change this to be a better value than just random whatever.
        byte[] data = new byte[(int) ((1024 * sampleRate * rowDurationInMs) / 1000) * (bitsPerSample / 8) * channels];
        int length = data.Length;
        for (int p = 0; p < _orders.Length; p++)
        {
            // I found that sometimes the order length returns an incorrect value and so will return orders with a value
            // of 255. Not sure what that's about, there's probably some reason for it. Anyway, if it's 255 we just
            // ignore it. Fairly sure most stuff doesn't have 255 orders anyway.
            if (_orders[p] == 255)
                continue;

            // Used for the "Pattern break" commands - if this value is set it will jump to the next pattern on the next
            // row change.
            bool shouldIncreasePattern = false;
            Pattern pattern = _patterns[_orders[p]];
            int row = 0;
            int tick = 0;
            bool rowChanged = true;
            while (row < pattern.Length)
            {
                // We should only look for new commands once the row changes, not when the tick changes.
                if (rowChanged)
                {
                    rowChanged = false;
                    tick = 0;
                    for (int c = 0; c < chn.Length; c++)
                    {
                        Note n = pattern.Notes[c, row];
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
                            chn[c].Key = n.Key;
                            chn[c].Octave = n.Octave;
                            chn[c].SampleID = (byte) n.SampleNum;
                            chn[c].SampleRate = (uint) (_samples[chn[c].SampleID].SampleRate * pn.Pitch);
                            chn[c].Volume = pn.Volume;
                            chn[c].Ratio = chn[c].SampleRate / (float) sampleRate;
                            chn[c].SamplePos = 0;
                        }
                        else
                        {
                            // TODO: Fix to allow effect commands that don't set volume to max (might not be caused by this bit of code though)
                            chn[c].Volume = n.Volume * PitchNote.RefVolume;
                        }

                        chn[c].Effect = Effect.None;

                        // Process various effects.
                        // Some can be processed in row, some are processed per tick.
                        switch (n.Effect)
                        {
                            case Effect.PatternBreak:
                                shouldIncreasePattern = true;
                                break;
                            case Effect.SetSpeed:
                                speed = n.EffectParam;
                                rowDurationInMs = (2500 / tempo) * speed;
                                break;
                            case Effect.SetTempo:
                                tempo = n.EffectParam;
                                rowDurationInMs = (2500 / tempo) * speed;
                                tickDurationInMs = (2500 / tempo);
                                break;
                            case Effect.PortamentoUp:
                                chn[c].Effect = Effect.PortamentoUp;
                                if (n.EffectParam != 0)
                                {
                                    // TODO: Fix pitch bending, it does NOT work at all, it's almost comical
                                    chn[c].EffectParam = (byte) n.EffectParam;
                                    PitchNote pn = new PitchNote(chn[c].Key, chn[c].Octave, 64);
                                    chn[c].Period = (pn.InverseKey * pn.InverseOctave) *
                                                    (3546895f / (pn.InverseKey * pn.InverseOctave));
                                    Console.WriteLine(chn[c].Period * PitchNote.Tuning);
                                }

                                break;
                        }
                    }
                }

                bool perTickEffect = false;
                for (int i = 0; i < (sampleRate * tickDurationInMs) / 1000; i++)
                {
                    for (int c = 0; c < chn.Length; c++)
                    {
                        // Process any per tick effects.
                        // TODO: Improve this bullcrap system of doing things, the code is messy!!
                        if (perTickEffect)
                        {
                            switch (chn[c].Effect)
                            {
                                case Effect.PortamentoUp:
                                    chn[c].SampleRate += (uint) (chn[c].Period * chn[c].EffectParam);
                                    chn[c].Ratio = chn[c].SampleRate / (float) sampleRate;
                                    break;
                            }
                        }
                        
                        // Since above we are working in samples it doesn't count for the stereo-ness of the sample.
                        // However since below we are working with array values we now need to account for this.
                        // Stereo samples need to be advanced twice as fast to account for the second "channel" of audio.
                        int multiplier = _samples[chn[c].SampleID].Stereo ? 2 : 1;
                        chn[c].SamplePos += chn[c].Ratio * multiplier;
                        if (_samples[chn[c].SampleID].Loop &&
                            chn[c].SamplePos >= _samples[chn[c].SampleID].LoopEnd * multiplier)
                        {
                            chn[c].SamplePos = _samples[chn[c].SampleID].LoopBegin * multiplier;
                        }
                        
                        // No need to calculate the stuff below, it would just be wasted computing time.
                        if (chn[c].Volume == 0)
                            continue;
                        
                        // The value +4 was chosen because it stopped crashing.
                        // At some point I'll look to properly do this. It works for now.
                        // Sometimes songs do a bad and tell it to use a null sample. Check for this and ignore.
                        // Also don't process audio outside the sample's data range.
                        // TODO: Check for null during loading?
                        if (_samples[chn[c].SampleID].Data != null && chn[c].SamplePos + 4 < _samples[chn[c].SampleID].Data.Length)
                        {
                            // Resampler and mix algorithm.
                            // Since our output audio is 16-bit stereo we must loop through the code twice to convert
                            // to stereo.
                            for (int a = 0; a < 4; a += 2)
                            {
                                // Get our already existing sample in the array.
                                // Again, since we are working with 16-bit, we convert to a signed short value.
                                short sample = (short) (data[dataPos + a] | data[dataPos + 1 + a] << 8);
                                short newSample;

                                // 8-bit values must be converted to 16-bit before the resampler can mix it together.
                                // Earlier versions of this code preprocessed the samples up to stereo and 16-bit because
                                // I didn't know how to do it during actual playback.
                                // The rest of this code just gets the sample at the correct position, by clamping our
                                // sample position to an integer value, and aligning it to the correct position if need
                                // be (so 16-bit samples will always be aligned at the start of the sample)
                                // TODO: This code assumes 8-bit samples are mono.
                                if (_samples[chn[c].SampleID].Type == SampleType.EightBit)
                                    newSample = (short) (((_samples[chn[c].SampleID].Data[(int) chn[c].SamplePos] << 8) - short.MaxValue));
                                else
                                    newSample = (short) ((_samples[chn[c].SampleID].Data[(int) chn[c].SamplePos - (int) chn[c].SamplePos % 2 + a] | _samples[chn[c].SampleID].Data[(int) chn[c].SamplePos - (int) chn[c].SamplePos % 2 + 1 + a] << 8) - short.MaxValue);

                                // Mix our sample together, using an integer in case the values overflow.
                                // The value 8 was chosen here because it prevents clipping.
                                // TODO: Divide by the total number of channels, not by 8.
                                int intSample = (int) (sample + (newSample / 8) * chn[c].Volume);
                                
                                // Clamp our integer to a 16-bit value to prevent over or underflowing.
                                intSample = intSample >= short.MaxValue ? short.MaxValue :
                                    intSample <= short.MinValue ? short.MinValue : intSample;
                                
                                short finalSample = (short) (intSample);

                                // Convert to our PCM data.
                                // Keep in mind here this data is 16-bit.
                                // The first byte must be our low byte so we do a bitwise AND of 255 to get the low
                                // part of the byte.
                                // The second byte must be our high byte, so we just shift down by 8 to get the high
                                // part of the byte.
                                data[dataPos + a] = (byte) (finalSample & 0xFF);
                                data[dataPos + 1 + a] = (byte) (finalSample >> 8);
                                
                                // And that's the resampler and mixer done! Quite simple, actually.
                                // Still remember when I was trying to work out how to do it and spent literally hours
                                // trying to get it to work!
                            }
                        }
                    }

                    // Increase our data position by 4 (as 16-bit stereo) so the next sample will be in the right place.
                    dataPos += 4;
                    
                    // Resize our array to yet another arbitrary value if our data is too large.
                    // While not idea, this is far better than the way it used to do it, resized every row!
                    // That became real slow real quick, could take up to a minute to process tracks.
                    if (dataPos >= length)
                    {
                        Array.Resize(ref data,
                            data.Length + (int) ((1024 * sampleRate * rowDurationInMs) / 1000) * (bitsPerSample / 8) *
                            channels);
                        length = data.Length;
                    }

                    perTickEffect = false;
                }

                // Increase our tick and row if need be.
                tick++;
                if (tick >= speed)
                {
                    rowChanged = true;
                    row++;
                    if (shouldIncreasePattern)
                        break;
                }
            }
        }
        
        // Our array is probably larger than the data needed, so we resize here so we don't have 30 seconds of empty
        // array sitting around in our audio.
        Array.Resize(ref data, dataPos);

        beginLoopPoint = 0;
        endLoopPoint = -1;
        return data;
    }

    private void FillBuffer()
    {
        
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
        public bool Stereo;
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
        public byte EffectParam;
        public float Period;
        public PianoKey Key;
        public Octave Octave;
    }
}