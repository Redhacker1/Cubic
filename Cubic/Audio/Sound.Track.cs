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
            _samples[i].SampleMultiplier = _samples[i].SampleRate / CalculateSampleRate(PianoKey.C, Octave.Octave4, _samples[i].SampleRate, 1);
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
        for (int i = 0; i < _channels.Length; i++)
            _channels[i].Enabled = true;
        _tickDurationInSamples = CalculateTickDurationInSamples(initialTempo);
        _currentRow = 0;
        _currentOrder = 0;
        _rowChanged = true;

        _speed = initialSpeed;
    }

    internal void LoadIT(byte[] data)
    {
        using MemoryStream memStream = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(memStream);

        if (new string(reader.ReadChars(4)) != "IMPM")
            throw new CubicException("Given IT file is not valid.");

        reader.ReadBytes(28); // Ignore song name and pattern highlight info.
        short numOrders = reader.ReadInt16();
        short numInstruments = reader.ReadInt16();
        short numSamples = reader.ReadInt16();
        short numPatterns = reader.ReadInt16();

        reader.ReadBytes(4); // Ignore tracker version

        short flags = reader.ReadInt16(); // TODO: Implement flags checking

        reader.ReadInt16(); // special

        byte globalVolume = reader.ReadByte();
        byte mixVolume = reader.ReadByte();
        byte initialSpeed = reader.ReadByte();
        byte initialTempo = reader.ReadByte();
        byte panningSep = reader.ReadByte();
        reader.ReadBytes(11); // Ignore pitch wheel depth and message stuff

        byte[] channelPans = reader.ReadBytes(64);
        byte[] channelVols = reader.ReadBytes(64);

        byte[] orders = reader.ReadBytes(numOrders);
        
        // TODO: Instruments
        string testStr = "";
        long pos = reader.BaseStream.Position;
        while (testStr != "IMPS")
        {
            try
            {
                testStr = new string(reader.ReadChars(4));
            }
            catch (Exception) { }
            reader.BaseStream.Position = ++pos;
        }

        reader.BaseStream.Position = pos - 1;
        _samples = new Sample[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            if (new string(reader.ReadChars(4)) != "IMPS")
                throw new CubicException("Error reading sample header");
            reader.ReadBytes(13); // Ignore dos filename and what I assume is a padding byte
            
            _samples[i].Volume = reader.ReadByte();
            byte sFlags = reader.ReadByte();
            _samples[i].SixteenBit = (sFlags & 2) == 2;
            _samples[i].Stereo = (sFlags & 4) == 4;
            _samples[i].Loop = (sFlags & 16) == 16;
            byte sVol = reader.ReadByte();
            reader.ReadBytes(28); // sample name
            _samples[i].Length = (uint) reader.ReadInt32();
            _samples[i].LoopBegin = (uint) reader.ReadInt32();
            _samples[i].LoopEnd = (uint) reader.ReadInt32();
            _samples[i].SampleRate = (uint) reader.ReadInt32();
            _samples[i].SampleMultiplier = _samples[i].SampleRate / CalculateSampleRate(PianoKey.C, Octave.Octave4, _samples[i].SampleRate, 1);
            int sSustainBegin = reader.ReadInt32();
            int sSustainEnd = reader.ReadInt32();
            int samplePointer = reader.ReadInt32();
            reader.ReadBytes(4); // TODO: implement vibrato, unsupported for now
            long currentPos = reader.BaseStream.Position;
            reader.BaseStream.Position = samplePointer;
            _samples[i].Data = reader.ReadBytes((int) _samples[i].Length * (_samples[i].Stereo ? 2 : 1) * (_samples[i].SixteenBit ? 2 : 1));
            reader.BaseStream.Position = currentPos;
            _samples[i].Signed = true;
        }

        (byte cNote, byte cInst, byte cVol, byte cCmd, byte cCmdVal, byte cMask)[] channels =
            new (byte cNote, byte cInst, byte cVol, byte cCmd, byte cCmdVal, byte cMask)[64];
        for (int i = 0; i < channels.Length; i++)
        {
            channels[i].cVol = 255;
        }

        int maxChannels = 0;
        
        _patterns = new Pattern[numPatterns];
        for (int p = 0; p < numPatterns; p++)
        {
            short length = reader.ReadInt16();
            short numRows = reader.ReadInt16();

            _patterns[p] = new Pattern(64, numRows);
            reader.ReadBytes(4);

            for (int r = 0; r < numRows; r++)
            {
                byte channelMarker = reader.ReadByte();
                while (channelMarker != 0)
                {
                    int channel = (channelMarker - 1) & 63;
                    if (channel > maxChannels)
                        maxChannels = channel;
                    byte mask = channels[channel].cMask;
                    if ((channelMarker & 128) == 128)
                    {
                        mask = reader.ReadByte();
                        channels[channel].cMask = mask;
                    }

                    byte note = 253;
                    byte instrument = 0;
                    byte volumeInfo = 65;
                    byte command = 0;
                    byte commandInfo = 0;
                    if ((mask & 1) == 1)
                    {
                        note = reader.ReadByte();
                        channels[channel].cNote = note;
                        volumeInfo = 64;
                    }

                    if ((mask & 2) == 2)
                    {
                        instrument = reader.ReadByte();
                        channels[channel].cInst = instrument;
                    }

                    if ((mask & 4) == 4)
                    {
                        volumeInfo = reader.ReadByte();
                        channels[channel].cVol = volumeInfo;
                    }

                    if ((mask & 8) == 8)
                    {
                        command = reader.ReadByte();
                        commandInfo = reader.ReadByte();
                        channels[channel].cCmd = command;
                        channels[channel].cCmdVal = commandInfo;
                    }

                    if ((mask & 16) == 16)
                    {
                        note = channels[channel].cNote;
                        volumeInfo = (byte) (volumeInfo == 65 ? 64 : volumeInfo);
                    }
                    if ((mask & 32) == 32)
                        instrument = channels[channel].cInst;
                    if ((mask & 64) == 64)
                        volumeInfo = channels[channel].cVol;
                    if ((mask & 128) == 128)
                    {
                        command = channels[channel].cCmd;
                        commandInfo = channels[channel].cCmdVal;
                    }

                    PianoKey key = PianoKey.None;
                    Octave octave = Octave.Octave0;
                    if (note != 253)
                    {
                        if (note == 254)
                        {
                            key = PianoKey.NoteCut;
                            octave = Octave.Octave0;
                        }
                        else
                        {
                            key = (PianoKey) (note % 12) + 2;
                            octave = (Octave) (note / 12) - 1;
                        }
                    }
                    
                    Effect effect = (Effect) command;

                    _patterns[p].SetNote(r, channel, new Note(key, octave, instrument - 1, volumeInfo, effect, commandInfo));
                    //Console.WriteLine($"Row: {r} | Channel: {channel} | Note: {key} | Octave: {octave} | Instrument: {instrument - 1} | Volume: {volumeInfo} | Effect: {effect} | Info: {commandInfo}");

                    channelMarker = reader.ReadByte();
                }
            }
        }
        
        _channels = new Channel[maxChannels + 1];
        for (int i = 0; i < maxChannels + 1; i++)
        {
            _channels[i].Enabled = channelPans[i] < 128;
            _channels[i].Volume = 64;
        }
        
        _tickDurationInSamples = CalculateTickDurationInSamples(initialTempo);
        _currentRow = 0;
        _currentOrder = 0;
        _rowChanged = true;
        _orders = orders;

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
                    _channels[c].Effect = n.Effect;
                    if (!n.Initialized)
                    {
                        continue;
                    }

                    if (n.Key == PianoKey.NoteCut)
                    {
                        _channels[c].SampleRate = 0;
                        _channels[c].Ratio = 0;
                        _channels[c].NoteVolume = 0;
                    }
                    else if (n.Key != PianoKey.None)
                    {
                        _channels[c].SampleId = (uint) n.SampleNum;
                        _channels[c].SampleRate = CalculateSampleRate(n.Key, n.Octave,
                            _samples[_channels[c].SampleId].SampleRate,
                            _samples[_channels[c].SampleId].SampleMultiplier);
                        _channels[c].Ratio = _channels[c].SampleRate / SampleRate;
                        _channels[c].SamplePos = 0;
                        _channels[c].RampVolume = 0;
                    }

                    if (n.Volume < 65)
                    {
                        _channels[c].NoteVolume = n.Volume;
                    }
                    // Hmm.
                    else if (n.Volume > 65)
                        _channels[c].NoteVolume = 64;

                    switch (n.Effect)
                    {
                        case Effect.SetSpeed:
                            _speed = n.EffectParam;
                            break;
                        case Effect.PositionJump:
                            _channels[c].MiscParam = (byte) n.EffectParam;
                            break;
                        case Effect.SetTempo:
                            _tickDurationInSamples = CalculateTickDurationInSamples(n.EffectParam);
                            break;
                        case Effect.SampleOffset:
                            _channels[c].SamplePos = _channels[c].OffsetParam * 256 + _channels[c].HighOffset * 65536;
                            break;
                        case Effect.SetChannelVolume:
                            _channels[c].Volume = n.EffectParam;
                            break;
                        case Effect.Special:
                            if (n.EffectParam is >= 0xA0 and <= 0xAF)
                                _channels[c].HighOffset = (byte) (n.EffectParam - 0xA0);
                            break;
                    }
                    
                    if (n.EffectParam != 0)
                    {
                        switch (n.Effect)
                        {
                            case Effect.PortamentoUp:
                            case Effect.PortamentoDown:
                                _channels[c].PitchParam = (byte) n.EffectParam;
                                break;
                            case Effect.VolumeSlide:
                                _channels[c].VolParam = (byte) n.EffectParam;
                                break;
                            case Effect.SampleOffset:
                                _channels[c].SamplePos = n.EffectParam * 256 + _channels[c].HighOffset * 65536;
                                _channels[c].OffsetParam = (byte) n.EffectParam;
                                break;
                            case Effect.VolumeSlideAndVibrato:
                                goto case Effect.VolumeSlide;
                            default:
                                _channels[c].MiscParam = (byte) n.EffectParam;
                                break;
                        }
                    }
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
                {
                    _channels[c].SamplePos = _samples[_channels[c].SampleId].LoopBegin * multiplier +
                                             (_channels[c].SamplePos -
                                              _samples[_channels[c].SampleId].LoopEnd * multiplier);
                }

                if (!_channels[c].Enabled || _channels[c].Volume == 0 || _channels[c].NoteVolume == 0)
                    continue;

                // 20 samples ramp up, based on openmpt
                if (_channels[c].RampVolume < 1)
                    _channels[c].RampVolume += 1 / 20f;

                if (_samples[_channels[c].SampleId].Data != null && _channels[c].SamplePos < _samples[_channels[c].SampleId].Length * multiplier)
                {
                    for (int a = 0; a < Channels; a++)
                    {
                        short sample = _trackBuffer[samp + a];

                        short newSample;

                        // TODO: Add proper support for 16-bit samples, as well as mono & stereo samples for both 8 and 16 bits.

                        if (_samples[_channels[c].SampleId].SixteenBit)
                        {
                            newSample = (short) (((_samples[_channels[c].SampleId].Data[(int) (_channels[c].SamplePos - _channels[c].SamplePos % 2) + a * (_samples[_channels[c].SampleId].Stereo ? 1 : 0) * _samples[_channels[c].SampleId].Length]) | _samples[_channels[c].SampleId].Data[(int) (_channels[c].SamplePos - _channels[c].SamplePos % 2) + 1 + a] << 8) - (_samples[_channels[c].SampleId].Signed ? 0 : short.MaxValue));
                        }
                        else
                        {
                            newSample = (short) ((_samples[_channels[c].SampleId].Data[(int) _channels[c].SamplePos + a * (_samples[_channels[c].SampleId].Stereo ? 1 : 0) * _samples[_channels[c].SampleId].Length] << 8) - (_samples[_channels[c].SampleId].Signed ? 0 : short.MaxValue));
                            if (_interpolation)
                            {
                                int nextPos = _channels[c].Ratio < 1 ? (int) (_channels[c].SamplePos + a * (_samples[_channels[c].SampleId].Stereo ? 1 : 0) * _samples[_channels[c].SampleId].Length + 1) : (int) (_channels[c].SamplePos + a * (_samples[_channels[c].SampleId].Stereo ? 1 : 0) * _samples[_channels[c].SampleId].Length + _channels[c].Ratio);
                                if (nextPos < _samples[_channels[c].SampleId].Data.Length)
                                {
                                    short nextSample = (short) ((_samples[_channels[c].SampleId].Data[nextPos] << 8) - (_samples[_channels[c].SampleId].Signed ? 0 : short.MaxValue));
                                    newSample = (short) CubicMath.Lerp(newSample, nextSample, (_channels[c].SamplePos - (int) _channels[c].SamplePos) / 1);
                                }
                            }
                        }
                        
                        int mixedSample = (int) (sample + newSample / 4 * (_channels[c].NoteVolume * _samples[_channels[c].SampleId].Volume * _channels[c].Volume * 128 / 262144) * (1 / 128f) * _channels[c].RampVolume);
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
                    switch (_channels[c].Effect)
                    {
                        case Effect.PortamentoUp:
                            if (_currentTick == 1)
                                continue;
                            //_channels[c].NoteFrequency *= MathF.Pow(2, 1 / 12f * 1 / 16f * _channels[c].PitchParam);
                            //_channels[c].SampleRate = _samples[_channels[c].SampleId].SampleRate * _channels[c].NoteFrequency * PitchNote.Tuning;
                            _channels[c].SampleRate *= MathF.Pow(2, 4 * _channels[c].PitchParam / 768f); 
                            _channels[c].Ratio = _channels[c].SampleRate / SampleRate;
                            break;
                        case Effect.PortamentoDown:
                            if (_currentTick == 1)
                                continue;
                            //_channels[c].NoteFrequency /= MathF.Pow(2, 1 / 12f * 1 / 16f * _channels[c].PitchParam);
                            //_channels[c].SampleRate = _samples[_channels[c].SampleId].SampleRate * _channels[c].NoteFrequency * PitchNote.Tuning;
                            _channels[c].SampleRate *= MathF.Pow(2, -4 * _channels[c].PitchParam / 768f); 
                            _channels[c].Ratio = _channels[c].SampleRate / SampleRate;
                            break;
                        case Effect.VolumeSlide:
                            if (_channels[c].VolParam < 16 && _currentTick != 1)
                                _channels[c].NoteVolume -= _channels[c].VolParam;
                            else if (_channels[c].VolParam > 0xF0 && _currentTick == 1)
                                _channels[c].NoteVolume -= _channels[c].VolParam;
                            else if (_channels[c].VolParam % 16 == 0 && _currentTick != 1)
                                _channels[c].NoteVolume += _channels[c].VolParam / 16;
                            

                            _channels[c].NoteVolume = CubicMath.Clamp(_channels[c].NoteVolume, 0, 64);
                            break;
                        case Effect.VolumeSlideAndVibrato:
                            goto case Effect.VolumeSlide;
                        case Effect.Retrigger:
                            if (_currentTick % _channels[c].MiscParam == 0)
                                _channels[c].SamplePos = 0;
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
                                _currentOrder = _channels[c].MiscParam;
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
        return (int) (2.5f / tempo * SampleRate);
    }

    public static float CalculateSampleRate(PianoKey key, Octave octave, float actualRate, float sampleMultiplier)
    {
        int note = 40 + (int) (key - 2) + (int) (octave - 4) * 12;
        float powNote = MathF.Pow(2, (note - 49f) / 12f);
        return actualRate * powNote * sampleMultiplier;
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
        public float SampleMultiplier;
        public bool Signed;
    }

    private struct Channel
    {
        public float SamplePos;
        public float Volume;
        public float Ratio;
        public float SampleRate;
        public uint SampleId;

        public Effect Effect;

        public byte VolParam;
        public byte PitchParam;
        public byte OffsetParam;
        public byte HighOffset;

        public byte MiscParam;

        public int NoteVolume;
        public float RampVolume;

        public bool Enabled;
    }
}