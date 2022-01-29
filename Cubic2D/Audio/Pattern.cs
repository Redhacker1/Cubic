using System;
using System.Timers;

namespace Cubic2D.Audio;

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
}