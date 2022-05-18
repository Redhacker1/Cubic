using System;
using System.IO;
using Cubic.Utilities;

namespace Cubic.Audio;

public partial class Sound
{
    internal void LoadS3M(byte[] data)
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
        _orders = reader.ReadBytes(orderCount);
        ushort[] ptrInstruments = new ushort[instrumentCount];
        for (int i = 0; i < instrumentCount; i++)
            ptrInstruments[i] = reader.ReadUInt16();

        ushort[] ptrPatterns = new ushort[patternPtrCount];
        for (int i = 0; i < patternPtrCount; i++)
            ptrPatterns[i] = reader.ReadUInt16();

        _samples = new Sample[instrumentCount];
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
            _samples[i].Length = reader.ReadUInt32();
            _samples[i].LoopBegin = reader.ReadUInt32();
            _samples[i].LoopEnd = reader.ReadUInt32();
            _samples[i].Volume = reader.ReadByte();
            reader.ReadBytes(2); // pack, not used
            byte flags = reader.ReadByte();
            _samples[i].Loop = (flags & 1) != 0;
            _samples[i].Stereo = (flags & 2) != 0;
            _samples[i].SixteenBit = (flags & 4) != 0;
            
            _samples[i].SampleRate = reader.ReadUInt32();
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
            _samples[i].Data = reader.ReadBytes(!_samples[i].Stereo ? (int) _samples[i].Length : (int) _samples[i].Length * 2);
        }

        _patterns = new Pattern[patternPtrCount];
        for (int i = 0; i < patternPtrCount; i++)
        {
            reader.BaseStream.Position = ptrPatterns[i] * 16;
            reader.ReadUInt16();
            _patterns[i] = new Pattern(32);
            for (int r = 0; r < _patterns[i].Length; r++)
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
                    byte volume = 65;
                    byte specialCommand = 255;
                    byte commandInfo = 0;

                    // Check each of our flags here to set each value.
                    // To learn more, read the S3M format doc I linked further up.
                    if ((flag & 32) != 0)
                    {
                        note = reader.ReadByte();
                        instrument = (byte) (reader.ReadByte() - 1);
                        volume = 64;
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
                        case 2:
                            effect = Effect.PositionJump;
                            break;
                        case 3:
                            effect = Effect.PatternBreak;
                            break;
                        case 4:
                            effect = Effect.VolumeSlide;
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

                    _patterns[i].SetNote(r, channel, new Note(key, octave, instrument, volume, effect, commandInfo));
                } while (flag != 0);
            }
        }

        _channels = new Channel[16];
        _tickDurationInSamples = CalculateTickDurationInSamples(initialTempo);
        _currentRow = 0;
        _currentOrder = 0;
        _rowChanged = true;

        _speed = initialSpeed;
    }

    private short[] _trackBuffer;
    private int _tickDurationInSamples;
    private int _sampleCount;
    private Channel[] _channels;
    private int _currentOrder;
    private int _currentRow;
    private int _currentTick;
    private int _speed;

    private Pattern[] _patterns;
    private Sample[] _samples;
    private byte[] _orders;
    private bool _rowChanged;
    private bool _interpolation;

    private void GetTrackData()
    {
        Array.Clear(_trackBuffer);
        for (int samp = 0; samp < _trackBuffer.Length; samp += Channels)
        {
            if (_rowChanged)
            {
                _rowChanged = false;
                _currentTick = 0;

                Pattern pattern = _patterns[_orders[_currentOrder]];
                for (int c = 0; c < _channels.Length; c++)
                {
                    Note n = pattern.Notes[c, _currentRow];
                    if (!n.Initialized)
                    {
                        _channels[c].Effect = Effect.None;
                        _channels[c].EffectParam = 0;
                        continue;
                    }

                    if (n.Key == PianoKey.NoteCut)
                    {
                        _channels[c].SampleRate = 0;
                        _channels[c].Ratio = 0;
                        _channels[c].Volume = 0;
                        continue;
                    }

                    if (n.Key != PianoKey.None)
                    {
                        PitchNote pn = new PitchNote(n.Key, n.Octave, n.Volume);
                        _channels[c].SampleId = (uint) n.SampleNum;
                        _channels[c].SampleRate = (float) (_samples[_channels[c].SampleId].SampleRate * pn.Pitch);
                            //CalculateSampleRate(n.Key, n.Octave, _samples[_channels[c].SampleId].SampleRate);
                            _channels[c].Ratio = _channels[c].SampleRate / SampleRate;
                        _channels[c].SamplePos = 0;
                    }

                    if (n.Volume != 65)
                    {
                        _channels[c].Volume = n.Volume * PitchNote.RefVolume;
                        _channels[c].NoteVolume = n.Volume;
                    }

                    switch (n.Effect)
                    {
                        case Effect.SetSpeed:
                            _speed = n.EffectParam;
                            break;
                    }
                    
                    _channels[c].Effect = n.Effect;
                    if (n.EffectParam != 0)
                        _channels[c].EffectParam = (byte) n.EffectParam;
                }
            }

            for (int c = 0; c < _channels.Length; c++)
            {
                if (_channels[c].SampleRate == 0)
                    continue;

                int multiplier = _samples[_channels[c].SampleId].SixteenBit ? 2 : 1;
                _channels[c].SamplePos += _channels[c].Ratio * multiplier;
                if (_samples[_channels[c].SampleId].Loop &&
                    _channels[c].SamplePos >= _samples[_channels[c].SampleId].LoopEnd * multiplier)
                    _channels[c].SamplePos = _samples[_channels[c].SampleId].LoopBegin * multiplier;
                
                if (_channels[c].Volume == 0)
                    continue;

                if (_samples[_channels[c].SampleId].Data != null &&
                    _channels[c].SamplePos < _samples[_channels[c].SampleId].Data.Length)
                {
                    for (int a = 0; a < Channels; a++)
                    {
                        short sample = _trackBuffer[samp + a];

                        short newSample;

                        // TODO: Add proper support for 16-bit samples, as well as mono & stereo samples for both 8 and 16 bits.
                        if (_samples[_channels[c].SampleId].SixteenBit)
                        {
                            newSample = (short) ((_samples[_channels[c].SampleId].Data[(int) _channels[c].SamplePos - (int) _channels[c].SamplePos % 2] | _samples[_channels[c].SampleId].Data[(int) _channels[c].SamplePos - (int) _channels[c].SamplePos % 2 + 1]  << 8) - short.MaxValue);
                        }
                        else
                        {
                            /*if (_samples[_channels[c].SampleId].Stereo)
                            {
                                // s3m files store samples weirdly - the first half of the data is the left channel, and
                                // the second half of the data is the right channel (i was expecting [l, r, l, r] etc.)
                                // therefore, we need to add the datalength / 2 every time a is 1 to get stereo.
                                newSample = (short) ((_samples[_channels[c].SampleId].Data[(a == 1 ? _samples[_channels[c].SampleId].Data.Length / 2 : 0) + ((int) _channels[c].SamplePos - (int) _channels[c].SamplePos % 2)] << 8) - short.MaxValue);
                            }
                            else
                            {*/
                            newSample = (short) ((_samples[_channels[c].SampleId].Data[(a == 1 && _samples[_channels[c].SampleId].Stereo ? _samples[_channels[c].SampleId].Data.Length / 2 : 0) + (int) _channels[c].SamplePos - (_samples[_channels[c].SampleId].Stereo ? (int) _channels[c].SamplePos % 2 : 0)] << 8) - short.MaxValue);
                                if (_interpolation)
                                {
                                    int nextPos = _channels[c].Ratio <= 1 ? (int) _channels[c].SamplePos + 1 : (int) (_channels[c].SamplePos + _channels[c].Ratio);
                                    short nextSample = (short) ((_samples[_channels[c].SampleId].Data[nextPos >= _samples[_channels[c].SampleId].Data.Length ? (int) _channels[c].SamplePos : nextPos] << 8) - short.MaxValue);
                                    newSample = (short) CubicMath.Lerp(newSample, nextSample, (_channels[c].SamplePos - (int) _channels[c].SamplePos) / 1);
                                }
                            //}
                        }

                        int mixedSample = (int) (sample + newSample / 4 * _channels[c].Volume);
                        mixedSample = CubicMath.Clamp(mixedSample, short.MinValue, short.MaxValue);

                        short shortSample = (short) mixedSample;

                        _trackBuffer[samp + a] = shortSample;
                    }
                }
            }
            
            _sampleCount++;

            if (_sampleCount >= _tickDurationInSamples)
            {
                _sampleCount = 0;
                _currentTick++;
                for (int c = 0; c < _channels.Length; c++)
                {
                    float freqUnit = 3546895 / _channels[c].SampleRate;
                    switch (_channels[c].Effect)
                    {
                        case Effect.PortamentoUp:
                            _channels[c].SampleRate += freqUnit * _channels[c].EffectParam;
                            _channels[c].Ratio = _channels[c].SampleRate / SampleRate;
                            break;
                        case Effect.VolumeSlide:
                            if (_channels[c].EffectParam < 16)
                                _channels[c].NoteVolume -= _channels[c].EffectParam;
                            else if (_channels[c].EffectParam % 16 == 0)
                                _channels[c].NoteVolume += _channels[c].EffectParam / 16;

                            _channels[c].NoteVolume = CubicMath.Clamp(_channels[c].NoteVolume, 0, 64);
                            _channels[c].Volume = _channels[c].NoteVolume * PitchNote.RefVolume;
                            break;
                    }
                }
                if (_currentTick >= _speed)
                {
                    _rowChanged = true;
                    _currentRow++;
                    for (int c = 0; c < _channels.Length; c++)
                    {
                        switch (_channels[c].Effect)
                        {
                            case Effect.PositionJump:
                                _currentOrder = _channels[c].EffectParam;
                                _currentRow = 0;
                                break;
                            case Effect.PatternBreak:
                                _currentRow = Int32.MaxValue;
                                break;
                        }
                    }
                    if (_currentRow >= _patterns[_orders[_currentOrder]].Length)
                    {
                        do
                        {
                            _currentRow = 0;
                            _currentOrder++;
                            if (_currentOrder >= _orders.Length)
                                _currentOrder = 0;
                        } while (_orders[_currentOrder] >= _patterns.Length);
                    }
                }
            }
        }
        
        _device.UpdateBuffer(_buffers[_currentBuffer], AudioFormat.Stereo16, _trackBuffer, SampleRate);
    }

    private int CalculateTickDurationInSamples(int tempo)
    {
        int tickDurationInMs = 2500 / tempo;
        return (int) ((tickDurationInMs / 1000f) * SampleRate);
    }

    private static int[] _periodTableS3M = { 1712, 1616, 1524, 1440, 1356, 1280, 1208, 1140, 1076, 1016, 0960, 0907 };
    
    private float CalculateSampleRate(PianoKey key, Octave octave, float cRate)
    {
        return 14317456f / (8363f * 16f * ((_periodTableS3M[(int) key - 2]) >> ((int) octave)) / cRate);
    }
    
    private struct Sample
    {
        public byte[] Data;
        public uint SampleRate;
        public uint Length;
        public uint LoopBegin;
        public uint LoopEnd;
        public uint Volume;
        public bool Stereo;
        public bool SixteenBit;
        public bool Loop;
    }

    private struct Channel
    {
        public float SamplePos;
        public float Volume;
        public float Ratio;
        public float SampleRate;
        public uint SampleId;

        public Effect Effect;
        public byte EffectParam;

        public int NoteVolume;
    }
}