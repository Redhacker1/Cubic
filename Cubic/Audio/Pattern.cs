using System.Text;

namespace Cubic.Audio;

internal struct Pattern
{
    public readonly Note[,] Notes;
    public readonly int Length;
    public readonly int NumChannels;

    public Pattern(int channels, int patternLength = 64)
    {
        Notes = new Note[channels, patternLength];
        Length = patternLength;
        NumChannels = channels;
    }

    public void SetNote(int index, int channel, Note note)
    {
        Notes[channel, index] = note;
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder("|");
        for (int c = 0; c < NumChannels; c++)
        {
            for (int r = 0; r < Length; r++)
            {
                Note n = Notes[c, r];
                string key = n.Key.ToString();
                if (n.Key == PianoKey.NoteCut)
                    key = "^^";
                else if (n.Key == PianoKey.None)
                    key = "..";
                string octave = n.Octave.ToString().Replace("Octave", "");
                builder.Append($"{key}{octave} {n.SampleNum} {n.Volume} ...|");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}