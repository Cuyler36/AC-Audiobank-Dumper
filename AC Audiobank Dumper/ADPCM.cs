namespace AC_Audiobank_Dumper
{
    public struct ADPCMLoop
    {
        public uint start;
        public uint end;
        public uint count;
        public short[] state;
        public uint unk;
    }

    public struct ADPCMBook
    {
        public uint order;
        public uint nPredictors;
        public short[] predictors;
    }

    public struct ADPCMWaveInfo
    {
        public ADPCMLoop loop;
        public ADPCMBook book;

        public ADPCMWaveInfo(in ADPCMLoop loop, in ADPCMBook book)
        {
            this.loop = loop;
            this.book = book;
        }
    }

    public struct RawLoop
    {
        public uint start;
        public uint end;
        public uint count;
    }
}
