using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Timers;
using Cubic2D.Scenes;

namespace Cubic2D.Audio;

public struct Track : IDisposable
{
    private string _title;
    private string _author;

    public string Title
    {
        get => _title;
        set
        {
            if (value.Length > 25)
                throw new Exception("Title can be 25 chars maximum.");
            _title = value;
        }
    }

    public string Author
    {
        get => _author;
        set
        {
            if (value.Length > 25)
                throw new Exception("Author can be 25 chars maximum.");
            _author = value;
        }
    }

    public int Tempo;
    public int Speed;

    private Pattern[] _patterns;

    private Sound[] _samples;

    private int _currentPattern;

    private int _currentRow;
    //private int[] _order;

    private Timer _timer;

    private int _interval;
    private long _lastMs;

    private AudioDevice _device;

    private Track(AudioDevice device, int tempo, int speed, Sound[] samples, Pattern[] patterns)
    {
        _patterns = patterns;
        _currentPattern = 0;
        _currentRow = 0;
        _interval = (2500 / tempo) * speed;
        _timer = new Timer(_interval);
        _device = device;
        _samples = samples;
        _lastMs = 0;
        _title = "";
        _author = "";
        Tempo = tempo;
        Speed = speed;
        _timer.Elapsed += TimerOnElapsed;
        SceneManager.Active.CreatedResources.Add(this);
    }

    public void Play()
    {
        _timer.Stop();
        _timer.Start();
    }
    
    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        Pattern pattern = _patterns[_currentPattern];
        bool skip = false;
        for (int i = 0; i < pattern.NumChannels; i++)
        {
            Note note = pattern.Notes[i, _currentRow];
            if (!note.Initialized)
                continue;
            switch (note.Key)
            {
                case PianoKey.None:
                    _device.SetVolume(i, note.Volume * PitchNote.RefVolume);
                    if (note.Effect == Effect.None)
                        continue;
                    switch (note.Effect)
                    {
                        case Effect.PositionJump:
                            _currentPattern = note.EffectParam;
                            _currentRow = 0;
                            skip = true;
                            break;
                    }
                    continue;
                case PianoKey.NoteOff:
                    _device.Stop(i);
                    continue;
                default:
                {
                    PitchNote pn = new PitchNote(note.Key, note.Octave, note.Volume);
                    _device.PlaySound(i, _samples[note.SampleNum], pn.Pitch, pn.Volume);
                    break;
                }
            }
        }

        if (skip)
            return;
        _currentRow++;
        if (_currentRow >= pattern.Length)
        {
            _currentRow = 0;
            _currentPattern++;
            if (_currentPattern >= _patterns.Length)
                _currentPattern = 0;
        }
        
        _device.Update();
    }
    
    public static Track LoadCtra(AudioDevice device, string path)
    {
        using DeflateStream deflateStream = new DeflateStream(File.OpenRead(path), CompressionMode.Decompress);
        using BinaryReader reader = new BinaryReader(deflateStream);
        if (new string(reader.ReadChars(10)) != "CUBICTRACK")
            throw new Exception("Given file is not a valid CTRA file.");
        if (reader.ReadUInt32() != 1)
            throw new Exception("Invalid version number");
        reader.ReadChars(25);
        reader.ReadChars(25);
        int tempo = reader.ReadByte();
        int speed = reader.ReadByte();
        if (new string(reader.ReadChars(7)) != "SAMPLES")
            throw new Exception("CTRA file is corrupt/invalid?");
        List<Sound> samples = new List<Sound>();
        List<Pattern> patterns = new List<Pattern>();
        int numSamples = reader.ReadByte();
        for (int i = 0; i < numSamples; i++)
        {
            reader.ReadByte();
            int sampleRate = (int) reader.ReadUInt32();
            int bitsPerSample = reader.ReadByte();
            int channels = reader.ReadByte();
            bool loop = reader.ReadBoolean();
            int beginLoop = 0;
            int endLoop = -1;
            if (loop)
            {
                beginLoop = (int) reader.ReadUInt32();
                endLoop = (int) reader.ReadUInt32();
            }

            int dataLength = (int) reader.ReadUInt32();
            byte[] data = reader.ReadBytes(dataLength);
            samples.Add(new Sound(data, channels, sampleRate, bitsPerSample, loop, beginLoop, endLoop));
        }

        if (new string(reader.ReadChars(8)) != "PATTERNS")
            throw new Exception("CTRA file is corrupt/invalid?");

        int numPatterns = reader.ReadByte();
        for (int pattern = 0; pattern < numPatterns; pattern++)
        {
            reader.ReadByte();
            int length = reader.ReadByte();
            int channels = reader.ReadByte();
            Pattern p = new Pattern(channels, length);
            for (int channel = 0; channel < channels; channel++)
            {
                for (int row = 0; row < length; row++)
                {
                    if (!reader.ReadBoolean())
                        continue;
                    PianoKey key = (PianoKey) reader.ReadByte();
                    Octave octave = (Octave) reader.ReadByte();
                    int sampleNum = reader.ReadByte();
                    int volume = reader.ReadByte();
                    Effect effect = (Effect) reader.ReadByte();
                    int effectParam = reader.ReadByte();

                    p.SetNote(row, channel, new Note(key, octave, sampleNum, volume, effect, effectParam));
                }
            }

            patterns.Add(p);
        }

        return new Track(device, tempo, speed, samples.ToArray(), patterns.ToArray());
    }

    public void Dispose()
    {
        foreach (Sound sound in _samples)
            sound.Dispose();
    }
}