using System;

namespace Cubic.Audio;

public ref struct PitchNote
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
    
    public double Pitch;
    public float Volume;

    public double InverseKey;
    public double InverseOctave;

    public PitchNote(PianoKey pianoKey, Octave octave, int volume)
    {
        int num = 0;
        InverseKey = pianoKey switch
        {
            PianoKey.C => 0,
            PianoKey.CSharp => 1,
            PianoKey.D => 2,
            PianoKey.DSharp => 3,
            PianoKey.E => 4,
            PianoKey.F => 5,
            PianoKey.FSharp => 6,
            PianoKey.G => 7,
            PianoKey.GSharp => 8,
            PianoKey.A => 9,
            PianoKey.ASharp => 10,
            PianoKey.B => 11,
            _ => throw new ArgumentOutOfRangeException(nameof(pianoKey), pianoKey, null)
        };

        InverseOctave = octave switch
        {
            Octave.Octave0 => -48,
            Octave.Octave1 => -36,
            Octave.Octave2 => -24,
            Octave.Octave3 => -12,
            Octave.Octave4 => 0,
            Octave.Octave5 => 12,
            Octave.Octave6 => 24,
            Octave.Octave7 => 36,
            Octave.Octave8 => 48,
            Octave.Octave9 => 60,
            _ => throw new ArgumentOutOfRangeException(nameof(octave), octave, null)
        };
        
        InverseKey = 440d * Math.Pow(Math.Pow(2, 1 / 12d), (InverseKey + InverseOctave) - 9);

        Pitch = InverseKey * 1d / (440d * Math.Pow(Math.Pow(2, 1 / 12d), -9));

        Volume = volume * RefVolume;
    }
}