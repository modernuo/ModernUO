using Server.Network;

namespace Server.Tests.Network
{
    public sealed class BondedStatus : Packet
    {
        public BondedStatus(Serial serial, bool bonded) : base(0xBF)
        {
            EnsureCapacity(11);

            Stream.Write((short)0x19);
            Stream.Write((byte)0);
            Stream.Write(serial);
            Stream.Write((byte)(bonded ? 1 : 0));
        }
    }

    public sealed class DeathAnimation : Packet
    {
        public DeathAnimation(Serial killed, Serial corpse) : base(0xAF, 13)
        {
            Stream.Write(killed);
            Stream.Write(corpse);
            Stream.Write(0);
        }
    }

    public sealed class MobileMoving : Packet
    {
        public MobileMoving(Mobile m, int noto, bool stygianAbyss) : base(0x77, 17)
        {
            var loc = m.Location;

            var hue = m.Hue;

            if (m.SolidHueOverride >= 0)
            {
                hue = m.SolidHueOverride;
            }

            Stream.Write(m.Serial);
            Stream.Write((short)m.Body);
            Stream.Write((short)loc.X);
            Stream.Write((short)loc.Y);
            Stream.Write((sbyte)loc.Z);
            Stream.Write((byte)m.Direction);
            Stream.Write((short)hue);
            Stream.Write((byte)m.GetPacketFlags(stygianAbyss));
            Stream.Write((byte)noto);
        }
    }
}
