using BinaryX;
using System.Runtime.InteropServices;

namespace AC_Audiobank_Dumper
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    [Endianness(ByteOrder.BigEndian)]
    public readonly struct AudioHeader
    {
        public readonly ushort NumItems;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    [Endianness(ByteOrder.BigEndian)]
    public readonly struct AudioHeaderItem
    {
        public readonly int RomOffset;
        public readonly int Size;
        public readonly byte CopyType;
    }
}
