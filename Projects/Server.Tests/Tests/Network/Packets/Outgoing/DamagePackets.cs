using System;

namespace Server.Network
{
    public sealed class DamagePacketOld : Packet
    {
        public DamagePacketOld(Serial mobile, int amount) : base(0xBF)
        {
            EnsureCapacity(11);

            Stream.Write((short)0x22);
            Stream.Write((byte)1);
            Stream.Write(mobile);

            Stream.Write((byte)Math.Clamp(amount, 0, 255));
        }
    }

    public sealed class DamagePacket : Packet
    {
        public DamagePacket(Serial mobile, int amount) : base(0x0B, 7)
        {
            Stream.Write(mobile);

            Stream.Write((ushort)Math.Clamp(amount, 0, 0xFFFF));
        }
    }
}
