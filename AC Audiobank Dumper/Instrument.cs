using BinaryX;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AC_Audiobank_Dumper
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
    [Endianness(ByteOrder.BigEndian)]
    public readonly struct Sound_s
    {
        public readonly byte flags;
        public readonly byte hasWavePrev;
        public readonly byte hasWaveSecondary;
        public readonly byte unk;
        public readonly int asdrDataOffset;
        public readonly int wavPrev;
        public readonly float keyBasePrev;
        public readonly int wav;
        public readonly float keyBase;
        public readonly int wavSecondary;
        public readonly float keyBaseSec;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    [Endianness(ByteOrder.BigEndian)]
    public readonly struct WaveTouch_s
    {
        public readonly int flags_size;
        public readonly int waveform_data_p;
        public readonly int loop_offset;
        public readonly int predictor_offset;

        public int GetFlags() => (flags_size >> 24) & 0xFF;
        public int GetSize() => flags_size & 0xFFFFFF;
    }

    public sealed class Waveform
    {
        public readonly WaveTouch_s WaveInfo;
        public readonly byte[] RawWaveData;
        public readonly ADPCMWaveInfo ADPCMWaveInfo;
        public readonly RawLoop RawLoop;

        public Waveform(BinaryReaderX controlBankReader, in WaveTouch_s waveTouch, List<Audiowave> waves)
        {
            WaveInfo = waveTouch;
            if (waveTouch.GetSize() == 0) return;
            RawWaveData = waves[(waveTouch.GetFlags() >> 2) & 3].GetInstrumentWaveform(waveTouch.waveform_data_p, waveTouch.GetSize());

            ADPCMWaveInfo = new ADPCMWaveInfo();

            if (waveTouch.loop_offset != 0)
            {
                controlBankReader.Seek(waveTouch.loop_offset);
                ADPCMLoop loop = new ADPCMLoop
                {
                    start = controlBankReader.ReadUInt32(),
                    end = controlBankReader.ReadUInt32(),
                    count = controlBankReader.ReadUInt32(),
                    unk = controlBankReader.ReadUInt32(),
                    state = new short[16]
                };

                if (loop.start != 0)
                {
                    for (int i = 0; i < 16; i++)
                        loop.state[i] = controlBankReader.ReadInt16();
                }

                ADPCMWaveInfo.loop = loop;
            }

            // Book
            if (waveTouch.predictor_offset != 0)
            {
                controlBankReader.Seek(waveTouch.predictor_offset);
                ADPCMBook book = new ADPCMBook
                {
                    order = controlBankReader.ReadUInt32(),
                    nPredictors = controlBankReader.ReadUInt32()
                };

                book.predictors = new short[book.order * book.nPredictors * 8];

                for (int i = 0; i < book.predictors.Length; i++)
                    book.predictors[i] = controlBankReader.ReadInt16();

                ADPCMWaveInfo.book = book;
            }
        }
    }

    public sealed class Sound
    {
        private readonly Sound_s soundInfo;

        public readonly Waveform WavePrevious;
        public readonly Waveform Wave;
        public readonly Waveform WaveSecondary;

        private readonly short[] adrsData;

        public Sound(BinaryReaderX controlBankReader, List<Audiowave> waves, Stack<long> _processStack)
        {
            _processStack.Push(controlBankReader.Position);
            soundInfo = controlBankReader.ReadStruct<Sound_s>();

            if (soundInfo.asdrDataOffset != 0)
            {
                Console.WriteLine($"\t\tASDR Offset: {soundInfo.asdrDataOffset:X}");
                adrsData = new short[8];
                controlBankReader.Seek(soundInfo.asdrDataOffset);
                for (int i = 0; i < 8; i++)
                    adrsData[i] = controlBankReader.ReadInt16();
            }

            if (soundInfo.hasWavePrev != 0)
            {
                controlBankReader.Seek(soundInfo.wavPrev);
                WaveTouch_s wavPrev = controlBankReader.ReadStruct<WaveTouch_s>();
                if (wavPrev.GetSize() != 0)
                    WavePrevious = new Waveform(controlBankReader, wavPrev, waves);
            }

            controlBankReader.Seek(soundInfo.wav);
            WaveTouch_s wav = controlBankReader.ReadStruct<WaveTouch_s>();
            Console.WriteLine($"\t\tUse Wave #{(wav.GetFlags() >> 2) & 3}");
            Console.WriteLine($"\t\tWave Offset: {wav.waveform_data_p:X}");
            Console.WriteLine($"\t\tWave Size: {wav.GetSize():X}");
            Console.WriteLine($"\t\tWave Loop Offset: {wav.loop_offset:X}");
            Console.WriteLine($"\t\tWave Predictors Offset: {wav.predictor_offset:X}");
            if (wav.GetSize() != 0)
                Wave = new Waveform(controlBankReader, wav, waves);
            if (soundInfo.hasWaveSecondary != 0x7F)
            {
                controlBankReader.Seek(soundInfo.wavSecondary);
                WaveTouch_s wavSecondary = controlBankReader.ReadStruct<WaveTouch_s>();
                if (wavSecondary.GetSize() != 0)
                    WaveSecondary = new Waveform(controlBankReader, wavSecondary, waves);
            }

            // Return Control Bank Reader to previous position
            controlBankReader.Seek(_processStack.Pop());
        }
    }

    public sealed class Instrument
    {
        public readonly Sound Sound;

        public Instrument(BinaryReaderX controlBankReader, List<Audiowave> waves, Stack<long> _processStack)
        {
            Sound = new Sound(controlBankReader, waves, _processStack);
        }
    }
}
