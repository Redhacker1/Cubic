using System;

namespace Cubic2D.Audio;

internal ref struct PitchNote
{
    #region Constants
    
    public const float Tuning = 1 / 261.6256f;
    public const float RefVolume = 1 / 64f;

    // All of these constants are precomputed to the correct frequency. This will drastically reduce the amount of
    // maths that needs to be done for each instrument! Before, I was doing this:
    // Pitch = MathF.Pow(2, (key - 49) / 12f) * 440 * Tuning;
    // Which is way slower!
    private const float C = 261.6256f;
    private const float CSharp = 277.1826f;
    private const float D = 293.6648f;
    private const float DSharp = 311.1270f;
    private const float E = 329.6276f;
    private const float F = 349.2282f;
    private const float FSharp = 369.9944f;
    private const float G = 391.9954f;
    private const float GSharp = 415.3047f;
    private const float A = 440.0000f;
    private const float ASharp = 466.1638f;
    private const float B = 493.8833f;

    private const float Octave0 = 1 / 16f;
    private const float Octave1 = 1 / 8f;
    private const float Octave2 = 1 / 4f;
    private const float Octave3 = 1 / 2f;
    private const float Octave4 = 1f;
    private const float Octave5 = 2f;
    private const float Octave6 = 4f;
    private const float Octave7 = 8f;
    private const float Octave8 = 16f;
    private const float Octave9 = 32f;

    #endregion
    
    public float Pitch;
    public float Volume;

    public float InverseKey;
    public float InverseOctave;

    public PitchNote(PianoKey pianoKey, Octave octave, int volume)
    {
        InverseKey = pianoKey switch
        {
            PianoKey.C => C,
            PianoKey.CSharp => CSharp,
            PianoKey.D => D,
            PianoKey.DSharp => DSharp,
            PianoKey.E => E,
            PianoKey.F => F,
            PianoKey.FSharp => FSharp,
            PianoKey.G => G,
            PianoKey.GSharp => GSharp,
            PianoKey.A => A,
            PianoKey.ASharp => ASharp,
            PianoKey.B => B,
            _ => throw new ArgumentOutOfRangeException(nameof(pianoKey), pianoKey, null)
        };

        InverseOctave = octave switch
        {
            Octave.Octave0 => Octave0,
            Octave.Octave1 => Octave1,
            Octave.Octave2 => Octave2,
            Octave.Octave3 => Octave3,
            Octave.Octave4 => Octave4,
            Octave.Octave5 => Octave5,
            Octave.Octave6 => Octave6,
            Octave.Octave7 => Octave7,
            Octave.Octave8 => Octave8,
            Octave.Octave9 => Octave9,
            _ => throw new ArgumentOutOfRangeException(nameof(octave), octave, null)
        };

        Pitch = InverseKey * Tuning * InverseOctave;

        Volume = volume * RefVolume;
    }
}