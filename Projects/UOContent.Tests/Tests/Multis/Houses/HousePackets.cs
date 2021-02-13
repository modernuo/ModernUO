namespace Server.Network
{
    public class BeginHouseCustomization : Packet
    {
        public BeginHouseCustomization(Serial house) : base(0xBF)
        {
            EnsureCapacity(17);

            Stream.Write((short)0x20);
            Stream.Write(house);
            Stream.Write((byte)0x04);
            Stream.Write((ushort)0x0000);
            Stream.Write((ushort)0xFFFF);
            Stream.Write((ushort)0xFFFF);
            Stream.Write((byte)0xFF);
        }
    }

    public class EndHouseCustomization : Packet
    {
        public EndHouseCustomization(Serial house) : base(0xBF)
        {
            EnsureCapacity(17);

            Stream.Write((short)0x20);
            Stream.Write(house);
            Stream.Write((byte)0x05);
            Stream.Write((ushort)0x0000);
            Stream.Write((ushort)0xFFFF);
            Stream.Write((ushort)0xFFFF);
            Stream.Write((byte)0xFF);
        }
    }

    public sealed class DesignStateGeneral : Packet
    {
        public DesignStateGeneral(Serial house, int revision) : base(0xBF)
        {
            EnsureCapacity(13);

            Stream.Write((short)0x1D);
            Stream.Write(house);
            Stream.Write(revision);
        }
    }
}
