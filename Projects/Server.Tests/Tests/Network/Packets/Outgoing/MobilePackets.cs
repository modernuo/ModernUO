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

    public sealed class MobileHits : Packet
    {
        public MobileHits(Mobile m) : base(0xA1, 9)
        {
            Stream.Write(m.Serial);
            Stream.Write((short)m.HitsMax);
            Stream.Write((short)m.Hits);
        }
    }

    public sealed class MobileHitsN : Packet
    {
        public MobileHitsN(Mobile m) : base(0xA1, 9)
        {
            Stream.Write(m.Serial);
            AttributeNormalizer.Write(Stream, m.Hits, m.HitsMax);
        }
    }

    public sealed class MobileMana : Packet
    {
        public MobileMana(Mobile m) : base(0xA2, 9)
        {
            Stream.Write(m.Serial);
            Stream.Write((short)m.ManaMax);
            Stream.Write((short)m.Mana);
        }
    }

    public sealed class MobileManaN : Packet
    {
        public MobileManaN(Mobile m) : base(0xA2, 9)
        {
            Stream.Write(m.Serial);
            AttributeNormalizer.Write(Stream, m.Mana, m.ManaMax);
        }
    }

    public sealed class MobileStam : Packet
    {
        public MobileStam(Mobile m) : base(0xA3, 9)
        {
            Stream.Write(m.Serial);
            Stream.Write((short)m.StamMax);
            Stream.Write((short)m.Stam);
        }
    }

    public sealed class MobileStamN : Packet
    {
        public MobileStamN(Mobile m) : base(0xA3, 9)
        {
            Stream.Write(m.Serial);
            AttributeNormalizer.Write(Stream, m.Stam, m.StamMax);
        }
    }

    public sealed class MobileAttributes : Packet
    {
        public MobileAttributes(Mobile m) : base(0x2D, 17)
        {
            Stream.Write(m.Serial);

            Stream.Write((short)m.HitsMax);
            Stream.Write((short)m.Hits);

            Stream.Write((short)m.ManaMax);
            Stream.Write((short)m.Mana);

            Stream.Write((short)m.StamMax);
            Stream.Write((short)m.Stam);
        }
    }

    public sealed class MobileAttributesN : Packet
    {
        public MobileAttributesN(Mobile m) : base(0x2D, 17)
        {
            Stream.Write(m.Serial);

            AttributeNormalizer.Write(Stream, m.Hits, m.HitsMax);
            AttributeNormalizer.Write(Stream, m.Mana, m.ManaMax);
            AttributeNormalizer.Write(Stream, m.Stam, m.StamMax);
        }
    }
}
