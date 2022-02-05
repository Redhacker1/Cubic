# The CTRA format
The Cubic Track (CTRA) format is the primary way Cubic stores its custom tracker files.

This document explains the file structure of a CTRA file.

**Note:** All version 1 CTRA files are compressed with a [DeflateStream](https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.deflatestream?view=net-6.0), with the [CompressionLevel](https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.compressionlevel?view=net-6.0) set to `Optimal`.

## Header
| Name        | Type   | SizeInBytes | Description                                                                    |
|-------------|--------|-------------|--------------------------------------------------------------------------------|
| Validator   | char[] | 20          | Each char is 2 bytes, this value should be `CUBICTRACK`.                       |
| Version     | uint   | 4           | The version number of this CTRA file. Use it to determine which loader to use. |
| TrackTitle  | char[] | 50          | The track's title, 25 chars in length.                                         |
| TrackAuthor | char[] | 50          | The track's author, 25 chars in length.                                        |
| TrackTempo  | byte   | 1           | The tempo of this track in BPM.                                                |
| TrackSpeed  | byte   | 1           | The speed of this track.                                                       |

## Samples
#### Sample header
| Name            | Type   | SizeInBytes | Description                                        |
|-----------------|--------|-------------|----------------------------------------------------|
| SampleValidator | char[] | 14          | Value should be `SAMPLES`                          |
| NumSamples      | byte   | 1           | The number of samples this track contains.         |
| SampleData      |        |             | See below. This data will repeat NumSamples times. |

#### Sample Data
| Name           | Type   | SizeInBytes | Description                                                                                    |
|----------------|--------|-------------|------------------------------------------------------------------------------------------------|
| SampleID       | byte   | 1           | The current Sample ID.                                                                         |
| SampleRate     | uint   | 4           | The sample rate of this sample.                                                                |
| BitsPerSample  | byte   | 1           | The number of bits per sample. This value will usually be 8 or 16.                             |
| Channels       | byte   | 1           | The number of channels this sample has. 1 = mono, 2 = stereo                                   |
| SampleLoop     | bool   | 1           | If true, the sample loops. **The next two bits of data are only present if this flag is set.** |
| BeginLoopPoint | uint   | 4           | The beginning loop point for this sample. This value only exists if the above flag is set.     |
| EndLoopPoint   | uint   | 4           | The ending loop point for this sample. This value only exists if the above flag is set.        |
| DataLength     | uint   | 4           | The total amount of bytes the data for this sample uses.                                       |
| SampleData     | byte[] | DataLength  | The waveform uncompressed data for this sample.                                                |

## Patterns
#### Pattern header
| Name             | Type   | SizeInBytes | Description                                 |
|------------------|--------|-------------|---------------------------------------------|
| PatternValidator | char[] | 16          | Value should be `PATTERNS`.                 |
| NumPatterns      | byte   | 1           | The total number of patterns in this track. |

#### Pattern data
| Name          | Type | SizeInBytes | Description                                                                                        |
|---------------|------|-------------|----------------------------------------------------------------------------------------------------|
| PatternID     | byte | 1           | The ID for this pattern.                                                                           |
| PatternLength | byte | 1           | The total number of rows this pattern has.                                                         |
| ChannelCount  | byte | 1           | The total number of channels this pattern has. (Thought: Shouldn't this really be a global const?) |
| NoteData      |      |             | See below. This data will repeat ChannelCount times.                                               |

#### Note & Channel data
This data goes rows then channels, so you should iterate something as such:
```
for (int channel = 0; channel < ChannelCount; channel++)
    for (int row = 0; row < PatternLength; row++)
        // Load each note here.
```
| Name            | Type | SizeInBytes | Description                                                                                                                                 |
|-----------------|------|-------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| NoteExists      | bool | 1           | If false, ignore **all** data below and skip this row.                                                                                      |
| NoteKey         | byte | 1           | This note's key. Use [PianoKey](https://github.com/ohtrobinson/Cubic2D/blob/master/Cubic2D/Audio/PianoKey.cs) as a reference for its value. |
| NoteOctave      | byte | 1           | This note's octave. Use [Octave](https://github.com/ohtrobinson/Cubic2D/blob/master/Cubic2D/Audio/Octave.cs) as a reference for its value.  |
| NoteSample      | byte | 1           | The note's SampleID.                                                                                                                        |
| NoteVolume      | byte | 1           | The volume of this note.                                                                                                                    |
| NoteEffect      | byte | 1           | The note's effect. Use [Effect](https://github.com/ohtrobinson/Cubic2D/blob/master/Cubic2D/Audio/Effect.cs) as a reference for its value.   |
| NoteEffectParam | byte | 1           | The note effect's parameter.                                                                                                                |