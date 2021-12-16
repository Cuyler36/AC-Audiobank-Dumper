using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AC_Audiobank_Dumper
{
    public static class ADPCMConverter
    {
        private static readonly short[] itable =
        {
            0,1,2,3,4,5,6,7,
            -8,-7,-6,-5,-4,-3,-2,-1,
        };

        private static readonly short[] itable_half =
        {
            0, 1,
            -2, -1,
        };

        private static short SignExtend16(int b, short x)
        {
            int m = 1 << (b - 1);
            x = (short)(x & ((1 << b) - 1));
            return (short)((x ^ m) - m);
        }

        private static void Decode_8(in byte[] adpcm, ref int readIdx, in short[] outData, ref int writeIdx, int index, in short[] pred1, in short[] lastsmp)
        {
            short[] tmp = new short[8];
            short[] pred2 = pred1.Skip(8).ToArray();

            //printf("pred2[] = %x\n" , pred2[0]);
            for (int i = 0; i < 8; i++)
            {
                tmp[i] = (short)(itable[(i & 1) == 1 ? (adpcm[readIdx++] & 0xf) : ((adpcm[readIdx] >> 4) & 0xf)] << index);
                tmp[i] = SignExtend16(index + 4, tmp[i]);
            }

	        for(int i=0; i<8; i++)
	        {
                int total = (pred1[i] * lastsmp[6]);
                total += (pred2[i] * lastsmp[7]);

		        if (i>0)
		        {
			        for(int x=i-1; x>-1; x--)
			        {
				        total += (tmp[((i - 1) - x)] * pred2[x] );
                        //printf("sample: %x - pred: %x - _smp: %x\n" , ((i-1)-x) , pred2[x] , tmp[((i-1)-x)]);
                    }
                }

                //printf("pred = %x | total = %x\n" , pred2[0] , total);
                float result = ((tmp[i] << 0xb) + total) >> 0xb;
                short sample;
                if (result > 32767)
                    sample = 32767;
                else if (result < -32768)
                    sample = -32768;
                else
                    sample = (short)result;

                outData[writeIdx++] = sample;
	        }

            // update the last sample set for subsequent iterations
            Buffer.BlockCopy(outData, writeIdx - 8, lastsmp, 0, sizeof(short) * 8);
        }

        private static void decode_8_half(in byte[] adpcm, ref int readIdx, in short[] outData, ref int writeIdx, int index, in short[] pred1, in short[] lastsmp)
        {
            int i;
            short[] tmp = new short[8];
            short[] pred2 = pred1.Skip(8).ToArray();

            //printf("pred2[] = %x\n" , pred2[0]);

            tmp[0] = (short)(((((adpcm[readIdx]) & 0xC0) >> 6) & 0x3) << index);
            tmp[0] = SignExtend16(index + 2, tmp[0]);
            tmp[1] = (short)(((((adpcm[readIdx]) & 0x30) >> 4) & 0x3) << index);
            tmp[1] = SignExtend16(index + 2, tmp[1]);
            tmp[2] = (short)(((((adpcm[readIdx]) & 0x0C) >> 2) & 0x3) << index);
            tmp[2] = SignExtend16(index + 2, tmp[2]);
            tmp[3] = (short)(((adpcm[readIdx++]) & 0x03 & 0x3) << index);
            tmp[3] = SignExtend16(index + 2, tmp[3]);
            tmp[4] = (short)(((((adpcm[readIdx]) & 0xC0) >> 6) & 0x3) << index);
            tmp[4] = SignExtend16(index + 2, tmp[4]);
            tmp[5] = (short)(((((adpcm[readIdx]) & 0x30) >> 4) & 0x3) << index);
            tmp[5] = SignExtend16(index + 2, tmp[5]);
            tmp[6] = (short)(((((adpcm[readIdx]) & 0x0C) >> 2) & 0x3) << index);
            tmp[6] = SignExtend16(index + 2, tmp[6]);
            tmp[7] = (short)(((adpcm[readIdx++]) & 0x03 & 0x3) << index);
            tmp[7] = SignExtend16(index + 2, tmp[7]);

            for (i = 0; i < 8; i++)
            {
                int total = (pred1[i] * lastsmp[6]);
                total += (pred2[i] * lastsmp[7]);

                if (i > 0)
                {
                    for (int x = i - 1; x > -1; x--)
                    {
                        total += (tmp[((i - 1) - x)] * pred2[x]);
                        //printf("sample: %x - pred: %x - _smp: %x\n" , ((i-1)-x) , pred2[x] , tmp[((i-1)-x)]);
                    }
                }

                //printf("pred = %x | total = %x\n" , pred2[0] , total);
                float result = ((tmp[i] << 0xb) + total) >> 0xb;
                short sample;
                if (result > 32767)
                    sample = 32767;
                else if (result < -32768)
                    sample = -32768;
                else
                    sample = (short)result;

                outData[writeIdx++] = sample;
            }
            // update the last sample set for subsequent iterations
            Buffer.BlockCopy(outData, writeIdx - 8, lastsmp, 0, sizeof(short) * 8);
        }

    private static (short[], int) DecodeADPCMData(in byte[] adpcm, ADPCMBook book, bool decode8Only)
        {
            short[] convertedData = new short[adpcm.Length * 4];
            short[] lastSmp = new short[8];
            int readIdx = 0;
            int writeIdx = 0;
            int samples = 0;

            if (!decode8Only)
            {
                int len = (adpcm.Length / 9) * 9;

                while (len > 0)
                {
                    int idx = (adpcm[readIdx] >> 4) & 0xF;
                    int pred = adpcm[readIdx] & 0xF;

                    // Uncomment if crash
                    pred %= (int)book.nPredictors;
                    len--;
                    short[] pred1 = new short[16];
                    Buffer.BlockCopy(book.predictors, pred * 16, pred1, 0, sizeof(short) * 16);

                    readIdx++;
                    Decode_8(adpcm, ref readIdx, convertedData, ref writeIdx, idx, pred1, lastSmp);
                    //readIdx += 4;
                    len -= 4;
                    //writeIdx += 8;

                    Decode_8(adpcm, ref readIdx, convertedData, ref writeIdx, idx, pred1, lastSmp);
                    //readIdx += 4;
                    len -= 4;
                    //writeIdx += 8;

                    samples += 16;
                }
            }
            else
            {
                int len = (adpcm.Length / 5) * 5;   //make sure length was actually a multiple of 5

                while (len > 0)
                {
                    int index = (adpcm[readIdx] >> 4) & 0xf;
                    int pred = (adpcm[readIdx] & 0xf);

                    // to not make zelda crash but doesn't fix it
                    pred %= (int)book.nPredictors;

                    len--;

                    short[] pred1 = new short[16];
                    Buffer.BlockCopy(book.predictors, pred * 16, pred1, 0, sizeof(short) * 16);

                    readIdx++;
                    decode_8_half(adpcm, ref readIdx, convertedData, ref writeIdx, index, pred1, lastSmp);
                    //readIdx += 2;
                    len -= 2;
                    //writeIdx += 8;

                    decode_8_half(adpcm, ref readIdx, convertedData, ref writeIdx, index, pred1, lastSmp);
                    //readIdx += 2;
                    len -= 2;
                    //writeIdx += 8;

                    samples += 16;
                }
            }

            return (convertedData, samples);
        }

        public static void ConvertToWAV(Instrument inst, in string filePath)
        {
            (short[] rawData, int samples) = DecodeADPCMData(inst.Sound.Wave.RawWaveData, inst.Sound.Wave.ADPCMWaveInfo.book, (inst.Sound.Wave.WaveInfo.GetFlags() & 0x30) != 0);
            using var writer = new BinaryWriter(File.Create(filePath));

            // Write WAV header.
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            int chunkSize = 0x28 + (samples * 2) + 0x2C - 0x8;
            if (inst.Sound.Wave.ADPCMWaveInfo.loop.count > 0)
                chunkSize += 0x18;
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16u); // 16 byte chunk size
            writer.Write((ushort)1); // wFormatTag -- 1 = PCM
            writer.Write((ushort)1); // nChannels
            writer.Write(32000); // sampling rate
            writer.Write(32000 * 2); // data rate -- bytes per second... not sure about this one.
            writer.Write((ushort)2); // Block alignment
            writer.Write((ushort)16); // Bits per sample
            // TODO: Aren't we missing some stuff? http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/WAVE.html

            // Write data tag
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(samples * 2); // Total samples

            // Write data
            for (int i = 0; i < rawData.Length; i++)
                writer.Write(rawData[i]);

            // Write loop if present
            if (inst.Sound.Wave.ADPCMWaveInfo.loop.count > 0)
            {
                writer.Write(Encoding.ASCII.GetBytes("smpl")); // Chunk type
                writer.Write(0x3C); // Chunk header size
                writer.Write(0); // Manufacturer
                writer.Write(0); // Product
                writer.Write(0); // Sample Period
                writer.Write(0x3C); // MIDI Unity Node
                writer.Write(0); // MIDI Pitch Fraction
                writer.Write(0); // SMPTE Format
                writer.Write(0); // SMPTE Offset
                writer.Write(1); // Num Sample Loops
                writer.Write(0); // Sampler Data
                // Sample List
                writer.Write(inst.Sound.Wave.ADPCMWaveInfo.loop.start);
                writer.Write(inst.Sound.Wave.ADPCMWaveInfo.loop.end);
                writer.Write(0); // Pad? idk
                if (inst.Sound.Wave.ADPCMWaveInfo.loop.count == uint.MaxValue)
                    writer.Write(0);
                else
                    writer.Write(inst.Sound.Wave.ADPCMWaveInfo.loop.count);
            }

            // Done writing WAV file.
            writer.Close();
        }
    }
}
