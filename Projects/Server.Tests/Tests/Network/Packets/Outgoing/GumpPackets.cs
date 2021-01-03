using Server.Network;

namespace Server.Tests
{
    public sealed class CloseGump : Packet
    {
        public CloseGump(int typeID, int buttonID) : base(0xBF)
        {
            EnsureCapacity(13);

            Stream.Write((short)0x04);
            Stream.Write(typeID);
            Stream.Write(buttonID);
        }
    }
}
