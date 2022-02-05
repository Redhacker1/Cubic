using System;
using System.IO;
using System.IO.Compression;
using OpenTK.Audio.OpenAL;

namespace Cubic2D.Audio;

public partial struct Sound
{
    public static byte[] LoadWav(Stream stream, out int channels, out int sampleRate, out int bitsPerSample)
    {
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Header
            
            if (new string(reader.ReadChars(4)) != "RIFF") // ChunkID
                throw new Exception("Given file is not a wave file.");
            
            reader.ReadInt32(); // ChunkSize
            
            if (new string(reader.ReadChars(4)) != "WAVE") // Format
                throw new Exception("Given wave file is not valid.");
            
            if (new string(reader.ReadChars(4)) != "fmt ") // Subchunk1ID
                throw new Exception("Given wave file is not valid.");

            reader.ReadInt32(); // Subchunk1Size
            
            if (reader.ReadInt16() != 1) // AudioFormat
                throw new Exception("Compressed wave files cannot be loaded.");

            channels = reader.ReadInt16();
            sampleRate = reader.ReadInt32();

            reader.ReadInt32(); // ByteRate, we just calculate this when needed.
            reader.ReadInt16(); // BlockAlign

            bitsPerSample = reader.ReadInt16();
            
            // Data
            
            if (new string(reader.ReadChars(4)) != "data") // Subchunk2ID
                throw new Exception("Given wave file is not valid.");

            int size = reader.ReadInt32(); // Subchunk2Size
            return reader.ReadBytes(size);
        }
    }

    public static byte[] LoadCtra(Stream stream, out int channels, out int sampleRate, out int bitsPerSample)
    {
        Console.WriteLine("CTRA->Sound converter");
        for (int i = 0; i < 25; i++)
            Console.Write('-');
        Console.WriteLine();
        using DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
        using BinaryReader reader = new BinaryReader(deflateStream);
        if (new string(reader.ReadChars(10)) != "CUBICTRACK")
            throw new CubicException("Given CTRA is not a cubic track.");
        reader.ReadUInt32();
        reader.ReadChars(25);
        reader.ReadChars(25);

        byte tempo = reader.ReadByte();
        Console.WriteLine("Tempo: " + tempo);
        byte speed = reader.ReadByte();
        Console.WriteLine("Speed: " + speed);

        if (new string(reader.ReadChars(7)) != "SAMPLES")
            throw new CubicException("Given CTRA file is not formed correctly (corrupted?).");
        byte numSamples = reader.ReadByte();
        Console.WriteLine("NumSamples: " + numSamples);
        Sample[] samples = new Sample[numSamples];
        for (int sample = 0; sample < numSamples; sample++)
        {
            Console.Write("Loading Sample " + sample + "... ");
            reader.ReadByte();
            samples[sample].SampleRate = reader.ReadUInt32();
            samples[sample].BitsPerSample = reader.ReadByte();
            samples[sample].Channels = reader.ReadByte();
            bool loop = reader.ReadBoolean();
            samples[sample].Loop = loop;
            if (loop)
            {
                samples[sample].BeginLoopPoint = reader.ReadUInt32();
                samples[sample].EndLoopPoint = reader.ReadUInt32();
            }

            uint dataLength = reader.ReadUInt32();
            samples[sample].Data = reader.ReadBytes((int) dataLength);
            samples[sample].Alignment = (byte) ((samples[sample].Channels * samples[sample].BitsPerSample) / 8);
            Console.WriteLine("Done");
        }
        
        if (new string(reader.ReadChars(8)) != "PATTERNS")
            throw new CubicException("Given CTRA file is not formed correctly (corrupted?).");

        byte numPatterns = reader.ReadByte();
        int totalRows = 0;
        Pattern[] patterns = new Pattern[numPatterns];
        Console.WriteLine("NumPatterns: " + numPatterns);
        for (int pattern = 0; pattern < numPatterns; pattern++)
        {
            Console.Write("Loading Pattern " + pattern + "... ");
            reader.ReadByte();
            byte pLength = reader.ReadByte();
            totalRows += pLength;
            byte pChannels = reader.ReadByte();
            Pattern p = new Pattern(pChannels, pLength);
            for (int channel = 0; channel < pChannels; channel++)
            {
                for (int row = 0; row < pLength; row++)
                {
                    if (!reader.ReadBoolean())
                        continue;
                    p.SetNote(row, channel,
                        new Note((PianoKey) reader.ReadByte(), (Octave) reader.ReadByte(), reader.ReadByte(),
                            reader.ReadByte(), (Effect) reader.ReadByte(), reader.ReadByte()));
                }
            }

            patterns[pattern] = p;
            Console.WriteLine("Done");
        }
        
        Console.WriteLine("CTRA loaded. Conversion starting...");

        channels = 2;
        bitsPerSample = 16;
        sampleRate = 44100;

        //tempo = 255;
        //speed = 1;

        int rowDurationInMs = (2500 / tempo) * speed;
        
        Console.WriteLine("Row duration in ms: " + rowDurationInMs);

        //int samplesPerRow = (sampleRate / 1000) * (sampleRate / rowDurationInMs);
        int samplesPerRow = (sampleRate * rowDurationInMs) / 1000;

        Console.WriteLine("Samples per row: " + samplesPerRow);
        int totalSamples = samplesPerRow * totalRows;
        Console.WriteLine("Song Length in seconds: " + totalSamples / sampleRate);

        // Create our full-length data! Since CTRA files can't change tempo (yet), this works fine for now.
        byte[] data = new byte[totalSamples * (bitsPerSample / 8) * channels];
        Console.WriteLine("Data Length: " + data.Length);
        uint alignment = (uint) ((channels * bitsPerSample) / 8);
        Console.WriteLine("Alignment: " + alignment);
        byte maxChannels = 0;
        foreach (Pattern p in patterns)
        {
            if (p.NumChannels > maxChannels)
                maxChannels = (byte) p.NumChannels;
        }
        
        // Resample and mix algorithm.
        // Not the fastest, but it currently processes each channel separately for the entire duration of the track and
        // mixes it with the current data as it progresses.
        for (int c = 0; c < maxChannels; c++)
        {
            // Since these values need to be reset each channel, it makes sense just to put this here.
            int currentPattern = 0;
            int currentRow = 0;
            int sampRate = 0;
            int rowI = 0;
            bool rowIncreased = true;
            uint samplePos = 0;
            int sampleNum = 0;
            float volume = 0;

            for (uint i = 0; i < data.Length; i += alignment)
            {
                // If the note has not been initialised, ignore it and continue, as the channel will just continue to
                // play whichever note and sample it has been assigned.
                if (rowIncreased && patterns[currentPattern].Notes[c, currentRow].Initialized)
                {
                    Note note = patterns[currentPattern].Notes[c, currentRow];
                    // Set the volume to the note's volume * the reference volume in PitchNote (1 / 64f)
                    volume = note.Volume * PitchNote.RefVolume;
                    // Just set volume & sample rate to 0, essentially "stopping" the note.
                    if (note.Key == PianoKey.NoteCut)
                    {
                        volume = 0;
                        sampRate = 0;
                    }
                    if (note.Key != PianoKey.None && note.Key != PianoKey.NoteCut)
                    {
                        // Calculate the volume and pitch multiplier for this sample.
                        PitchNote pn = new PitchNote(note.Key, note.Octave, note.Volume);
                        volume = pn.Volume;
                        sampleNum = note.SampleNum;
                        // Multiply the sample's sample rate by our pitch multiplier to get the output sample rate.
                        sampRate = (int) (samples[sampleNum].SampleRate * pn.Pitch);
                        // Reset our sample position too to retrigger any samples.
                        samplePos = 0;
                    }

                    rowIncreased = false;
                }

                // This will help us determine dataPoint, which is used to determine which bytes of the sample to add to
                // our data array.
                float ratio = sampleRate / (float) sampRate;
                // dataPoint calculates the exact starting sample data array index for any given value of i, based on
                // our computed ratio. It is aligned to the correct byte, so dataPoint + alignment will always be the
                // next sample in the array.
                int dataPoint = (int) ((samplePos * 1 / ratio) - (samplePos * 1 / ratio) % alignment);

                // This statement just prevents any potential overflows with our datapoint, which, with strange sample
                // rates, can occur.
                if (dataPoint < samples[sampleNum].Data.Length)
                {
                    // TODO: Implement checks for 8 bit as well.
                    // Our alignment value actually tells us how many bytes per sample there are, so for a 2-channel
                    // 16-bit sample, there will be 4 bytes per sample ((2 * 16) / 8). Because of this, we need to append
                    // the data 4 times. As this code currently assumes it is 16-bit, we always do 2 bytes at once.
                    // If the alignment is 4, this will repeat twice, for both channels.
                    for (int a = 0; a < alignment; a += 2)
                    {
                        // Convert our two bytes of data into a single short (16-bit) signed value.
                        // The data is little-endian, so we left shift the second byte by 8 to get our 16-bit value.
                        // Do this for both the current data in our data array, and the data in our sample data array.
                        short dataSample = (short) ((data[i + a]) | (data[i + a + 1] << 8));
                        short currentSample = (short) ((samples[sampleNum].Data[dataPoint + a]) |
                                                         (samples[sampleNum].Data[dataPoint + a + 1] << 8));
                        // Add the two values together, dividing the currentSample by the number of channels we have
                        // (and multiplying it by the volume multiplier) to ensure that it does not clip.
                        short final = (short) (dataSample + (currentSample / (float) maxChannels) * volume);

                        // Finally, we convert our calculated 16-bit value back into two little-endian 8-bit values
                        // that a wave format can hold.
                        data[i + a] = (byte) (final & 0xFF);
                        data[i + a + 1] = (byte) (final >> 8);
                    }
                }

                // Increase our samplePos by our alignment to ensure we always are at a new sample.
                samplePos += alignment;

                if (samples[sampleNum].Loop)
                {
                    // If the datapoint is more than the ending loop point of the sample, reset it back to the beginning
                    // loop point of the sample. (we need to divide by alignment here as we are working with samples and
                    // not bytes).
                    if (dataPoint >= samples[sampleNum].EndLoopPoint)
                        samplePos = samples[sampleNum].BeginLoopPoint / samples[sampleNum].Alignment;
                }

                // Yet another "i" value, this one helps us determine which row we are on.
                rowI++;

                // Our calculated samplesPerRow value is used here, this helps us determine which row of the pattern we
                // are on.
                if (rowI > samplesPerRow)
                {
                    rowI = 0;
                    currentRow++;
                    rowIncreased = true;
                }

                // Increase our pattern number if our row exceeds its length.
                if (currentRow >= patterns[currentPattern].Length)
                {
                    currentPattern++;
                    if (currentPattern >= patterns.Length)
                        break;
                    currentRow = 0;
                    rowIncreased = true;
                }
            }
        }

        // So far no errors yet, hopefully this error will never happen!
        ALError e;
        if ((e = AL.GetError()) != ALError.NoError)
            throw new CubicException($"Error while loading CTRA file: {e}");
        
        Console.WriteLine("Conversion complete!\nCTRA loaded and converted to Sound.");
        return data;
    }
    
    // We can't use the "Sound" struct here as that constructs a buffer for each sound, which is a waste of memory and
    // processing time.
    private struct Sample
    {
        public byte[] Data;
        public uint SampleRate;
        public byte BitsPerSample;
        public byte Channels;
        public byte Alignment;
        public bool Loop;
        public uint BeginLoopPoint;
        public uint EndLoopPoint;
    }
}