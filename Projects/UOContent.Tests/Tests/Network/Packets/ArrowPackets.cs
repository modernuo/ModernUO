namespace Server.Network
{
    public sealed class CancelArrow : Packet
    {
        public CancelArrow() : base(0xBA, 6)
        {
            Stream.Write((byte)0);
            Stream.Write((short)-1);
            Stream.Write((short)-1);
        }
    }

    public sealed class SetArrow : Packet
    {
        public SetArrow(int x, int y) : base(0xBA, 6)
        {
            Stream.Write((byte)1);
            Stream.Write((short)x);
            Stream.Write((short)y);
        }
    }

    public sealed class CancelArrowHS : Packet
    {
        public CancelArrowHS(int x, int y, Serial s) : base(0xBA, 10)
        {
            Stream.Write((byte)0);
            Stream.Write((short)x);
            Stream.Write((short)y);
            Stream.Write(s);
        }
    }

    public sealed class SetArrowHS : Packet
    {
        public SetArrowHS(int x, int y, Serial s) : base(0xBA, 10)
        {
            Stream.Write((byte)1);
            Stream.Write((short)x);
            Stream.Write((short)y);
            Stream.Write(s);
        }
    }
}
