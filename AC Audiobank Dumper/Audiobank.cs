using BinaryX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AC_Audiobank_Dumper
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    [Endianness(ByteOrder.BigEndian)]
    public readonly struct AudiobankEntry
    {
        public readonly int Offset;             // 00 - Offset of control table data
        public readonly int Size;               // 04 - Size of the control table data
        public readonly ushort Unk1;            // 08 - Unknown
        public readonly byte WaveTableIndex1;   // 0A - Wave Table 1 Index
        public readonly byte WaveTableIndex2;   // 0B - Wave Table 2 Index
        public readonly byte InstrumentCount;   // 0C - Total Instrument Count
        public readonly byte InstrumentCount1;  // 0D - Instrument Count for "Section 1" -- Percussions
        public readonly byte Unk2;              // 0E - Unknown
        public readonly byte InstrumentCount2;  // 0F - Instrument Count for "Section 2"
    }

    public sealed class AudioBank
    {
        public readonly AudiobankEntry HeaderInfo;
        public readonly List<Audiowave> Waves = new List<Audiowave>();
        public readonly List<Instrument> Instruments = new List<Instrument>();

        private readonly int bank_idx;
        private readonly byte[] _controlData;

        public AudioBank(BinaryReaderX headerReader, BinaryReaderX audioromReader, int bankStartOffset)
        {
            HeaderInfo = headerReader.ReadStruct<AudiobankEntry>();
            if (HeaderInfo.Size == 0)
            {
                Console.WriteLine($"Control Bank #{(headerReader.Position - 16) / 16:X} is linked to Control Bank #{HeaderInfo.Offset:X}! Not implemented yet, so skipping processing.");
                return;
            }
            else
            {
                Console.WriteLine($"Processing Control Bank #{(headerReader.Position - 16) / 16:X}");
            }

            bank_idx = (int)(headerReader.Position - 16) / 16;

            long preAddr = audioromReader.Position;
            audioromReader.Seek(bankStartOffset + HeaderInfo.Offset);
            _controlData = audioromReader.ReadBytes(HeaderInfo.Size);
            audioromReader.Seek(preAddr);

            if (HeaderInfo.WaveTableIndex1 != 0xFF)
                Waves.Add(Audiowave.GetWave(HeaderInfo.WaveTableIndex1));
            if (HeaderInfo.WaveTableIndex2 != 0xFF)
                Waves.Add(Audiowave.GetWave(HeaderInfo.WaveTableIndex2));

            ProcessData();
        }

        private Instrument ProcessInstrument(BinaryReaderX reader, int offset, Stack<long> processStack)
        {
            processStack.Push(reader.Position);
            reader.Seek(offset);
            Instrument inst = new Instrument(reader, Waves, processStack);
            Instruments.Add(inst);
            reader.Seek(processStack.Pop());
            return inst;
        }

        private void ProcessData()
        {
            using BinaryReaderX reader = new BinaryReaderX(new MemoryStream(_controlData), ByteOrder.BigEndian);
            // TODO: Percussion instruments or w/e the first two offsets are.
            reader.Seek(8);
            int offset;
            int instNum = 0;
            string path = Path.Combine(@"C:\Users\olsen\DnMe+ Inst Banks\", $"{bank_idx}");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            do
            {
                reader.Seek(8 + instNum * 4);
                offset = reader.ReadInt32();
                if (offset > 0)
                {
                    Console.WriteLine($"\tProcessing instrument {instNum++ + 1}/{HeaderInfo.InstrumentCount} @ 0x{offset:X}");
                    Instrument inst = ProcessInstrument(reader, offset, new Stack<long>());
                    ADPCMConverter.ConvertToWAV(inst, Path.Combine(path, $"inst_{instNum}.wav"));
                }
                else
                {
                    Console.WriteLine($"\tInstrument # {instNum++ + 1}/{HeaderInfo.InstrumentCount} had an offset of 0. Skipping.");
                    Instruments.Add(null);
                }
            } while (instNum < HeaderInfo.InstrumentCount);
            Console.WriteLine();
        }
    }
}
