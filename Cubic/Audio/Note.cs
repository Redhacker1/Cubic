namespace Cubic.Audio;

/// <summary>
/// Represents a note used in a tracker channel.
/// </summary>
public struct Note
{
    /// <summary>
    /// The key of this note.
    /// </summary>
    public readonly PianoKey Key;
    
    /// <summary>
    /// The octave of this note.
    /// </summary>
    public readonly Octave Octave;
    
    /// <summary>
    /// The sample number of this note.
    /// </summary>
    public readonly int SampleNum;
    
    /// <summary>
    /// The volume (between 0 and 64) of this note.
    /// </summary>
    public readonly int Volume;
    
    /// <summary>
    /// If not initialized, this note will be ignored.
    /// </summary>
    public readonly bool Initialized;
    
    /// <summary>
    /// The effect of this note, if any.
    /// </summary>
    public readonly Effect Effect;
    
    /// <summary>
    /// The effect parameter of this note, if any.
    /// </summary>
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