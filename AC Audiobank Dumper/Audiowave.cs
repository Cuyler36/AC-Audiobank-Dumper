using BinaryX;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AC_Audiobank_Dumper
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    [Endianness(ByteOrder.BigEndian)]
    public readonly struct AudiowaveEntry
    {
        public readonly int Offset;             // 00 - Offset of wave table data
        public readonly int Size;               // 04 - Size of the wave table data
        public readonly ushort Unk1;            // 08 - Unknown
    }

    public sealed class Audiowave
    {
        private static readonly List<Audiowave> AudioWaves = new List<Audiowave>();

        public static Audiowave GetWave(int waveId)
        {
            if (waveId >= AudioWaves.Count)
                throw new ArgumentOutOfRangeException(nameof(waveId));
            return AudioWaves[waveId];
        }

        public readonly AudiowaveEntry HeaderInfo;

        private readonly byte[] _waveformData;

        public Audiowave(BinaryReaderX headerReader, BinaryReaderX audioromReader, int waveBaseOffset)
        {
            long preAddr = audioromReader.Position;
            HeaderInfo = headerReader.ReadStruct<AudiowaveEntry>();
            audioromReader.Seek(waveBaseOffset + HeaderInfo.Offset);
            _waveformData = audioromReader.ReadBytes(HeaderInfo.Size);
            audioromReader.Seek(preAddr);

            AudioWaves.Add(this);
        }

        public byte[] GetInstrumentWaveform(int offset, int size)
        {
            byte[] copyData = new byte[size];
            Buffer.BlockCopy(_waveformData, offset, copyData, 0, size);
            return copyData;
        }
    }
}
