namespace Cubic.Audio;

internal struct Note
{
    public readonly PianoKey Key;
    public readonly Octave Octave;
    public readonly int SampleNum;
    public readonly int Volume;
    public readonly bool Initialized;
    public readonly Effect Effect;
    public readonly int EffectParam;

    public Note(PianoKey key, Octave octave, int sampleNum, int volume = 0, Effect effect = Effect.None, int effectParam = 0)
    {
        Key = key;
        Octave = octave;
        SampleNum = sampleNum;
        Volume = volume;
        Effect = effect;
        EffectParam = effectParam;
        Initialized = true;
    }
}