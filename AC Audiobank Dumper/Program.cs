using BinaryX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AC_Audiobank_Dumper
{
    class Program
    {
        private static Stack<long> _processStack = new Stack<long>();

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Enter the path to the directory holding the audio rom files:");
                args = new string[] { Console.ReadLine().Replace("\"", "") };
            }

            if (!Directory.Exists(args[0]))
                throw new DirectoryNotFoundException();

            string dataPath = Path.Combine(args[0], "AudiodataHeaderStart.bin");
            string bankPath = Path.Combine(args[0], "AudiobankHeaderStart.bin");
            string wavePath = Path.Combine(args[0], "AudiowaveHeaderStart.bin");
            string seqPath = Path.Combine(args[0], "AudioseqHeaderStart.bin");
            string romPath = Path.Combine(args[0], "audiorom.img");

            if (!File.Exists(dataPath))
                throw new FileNotFoundException($"{dataPath} not found!");
            if (!File.Exists(bankPath))
                throw new FileNotFoundException($"{bankPath} not found!");
            if (!File.Exists(wavePath))
                throw new FileNotFoundException($"{wavePath} not found!");
            if (!File.Exists(seqPath))
                throw new FileNotFoundException($"{seqPath} not found!");
            if (!File.Exists(romPath))
                throw new FileNotFoundException($"{romPath} not found!");

            using BinaryReaderX dataReader = new BinaryReaderX(File.OpenRead(dataPath), ByteOrder.BigEndian);
            using BinaryReaderX bankReader = new BinaryReaderX(File.OpenRead(bankPath), ByteOrder.BigEndian);
            using BinaryReaderX waveReader = new BinaryReaderX(File.OpenRead(wavePath), ByteOrder.BigEndian);
            using BinaryReaderX romReader = new BinaryReaderX(File.OpenRead(romPath), ByteOrder.BigEndian);

            // Begin by confirming that there are 3 entries in AudiodataHeaderStart.bin's header
            if (dataReader.ReadStruct<AudioHeader>().NumItems != 3)
                throw new InvalidDataException("Bad number of entries in AudiodataHeaderStart.bin Header! Should be 3.");

            // Sequence -> Control Bank -> Waveform
            AudioHeaderItem sequenceHeaderInfo = dataReader.ReadStruct<AudioHeaderItem>();
            AudioHeaderItem cntlBankHeaderInfo = dataReader.ReadStruct<AudioHeaderItem>();
            AudioHeaderItem waveformHeaderInfo = dataReader.ReadStruct<AudioHeaderItem>();

            // TODO: Audio Sequence Dumps Last.

            // We MUST extract the waves first, they're used in banks. Then banks are used in sequences.
            int numWaves = waveReader.ReadStruct<AudioHeader>().NumItems;
            for (int i = 0; i < numWaves; i++)
                new Audiowave(waveReader, romReader, waveformHeaderInfo.RomOffset);

            // Now, process all instrument control banks.
            int numBanks = bankReader.ReadStruct<AudioHeader>().NumItems;
            for (int i = 0; i < numBanks; i++)
            {
                AudioBank instBank = new AudioBank(bankReader, romReader, cntlBankHeaderInfo.RomOffset);
            }

            Console.WriteLine("Done reading audio control banks!");
        }
    }
}
