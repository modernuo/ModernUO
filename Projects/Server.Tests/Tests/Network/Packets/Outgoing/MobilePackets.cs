using System.Threading;
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

    public sealed class MobileName : Packet
    {
        public MobileName(Mobile m) : base(0x98)
        {
            EnsureCapacity(37);

            Stream.Write(m.Serial);
            Stream.WriteAsciiFixed(m.Name ?? "", 29);
            Stream.Write((byte)0); // Null terminator
        }
    }

    public sealed class MobileAnimation : Packet
    {
        public MobileAnimation(
            Serial mobile, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay
        ) : base(0x6E, 14)
        {
            Stream.Write(mobile);
            Stream.Write((short)action);
            Stream.Write((short)frameCount);
            Stream.Write((short)repeatCount);
            Stream.Write(!forward); // protocol has really "reverse" but I find this more intuitive
            Stream.Write(repeat);
            Stream.Write((byte)delay);
        }
    }

    public sealed class NewMobileAnimation : Packet
    {
        public NewMobileAnimation(Serial mobile, int action, int frameCount, int delay) : base(0xE2, 10)
        {
            Stream.Write(mobile);
            Stream.Write((short)action);
            Stream.Write((short)frameCount);
            Stream.Write((byte)delay);
        }
    }

    public sealed class HealthbarPoison : Packet
    {
        public HealthbarPoison(Mobile m) : base(0x17)
        {
            EnsureCapacity(12);

            Stream.Write(m.Serial);
            Stream.Write((short)1); // Show Bar?

            Stream.Write((short)1); // Poison Bar

            var p = m.Poison;

            if (p != null)
            {
                Stream.Write((byte)(p.Level + 1));
            }
            else
            {
                Stream.Write((byte)0);
            }
        }
    }

    public sealed class HealthbarYellow : Packet
    {
        public HealthbarYellow(Mobile m) : base(0x17)
        {
            EnsureCapacity(12);

            Stream.Write(m.Serial);
            Stream.Write((short)1);

            Stream.Write((short)2);

            if (m.Blessed || m.YellowHealthbar)
            {
                Stream.Write((byte)1);
            }
            else
            {
                Stream.Write((byte)0);
            }
        }
    }

    public sealed class MobileStatusCompact : Packet
    {
        public MobileStatusCompact(bool canBeRenamed, Mobile m) : base(0x11)
        {
            EnsureCapacity(43);

            Stream.Write(m.Serial);
            Stream.WriteAsciiFixed(m.Name ?? "", 30);

            AttributeNormalizer.WriteReverse(Stream, m.Hits, m.HitsMax);

            Stream.Write(canBeRenamed);

            Stream.Write((byte)0); // type
        }
    }

    public sealed class MobileStatusExtended : Packet
    {
        public MobileStatusExtended(Mobile m) : this(m, m.NetState)
        {
        }

        public MobileStatusExtended(Mobile m, NetState ns) : base(0x11)
        {
            var name = m.Name ?? "";

            int type;

            if (Core.HS && ns?.ExtendedStatus == true)
            {
                type = 6;
                EnsureCapacity(121);
            }
            else if (Core.ML && ns?.SupportsExpansion(Expansion.ML) == true)
            {
                type = 5;
                EnsureCapacity(91);
            }
            else
            {
                type = Core.AOS ? 4 : 3;
                EnsureCapacity(88);
            }

            Stream.Write(m.Serial);
            Stream.WriteAsciiFixed(name, 30);

            Stream.Write((short)m.Hits);
            Stream.Write((short)m.HitsMax);

            Stream.Write(m.CanBeRenamedBy(m));

            Stream.Write((byte)type);

            Stream.Write(m.Female);

            Stream.Write((short)m.Str);
            Stream.Write((short)m.Dex);
            Stream.Write((short)m.Int);

            Stream.Write((short)m.Stam);
            Stream.Write((short)m.StamMax);

            Stream.Write((short)m.Mana);
            Stream.Write((short)m.ManaMax);

            Stream.Write(m.TotalGold);
            Stream.Write((short)(Core.AOS ? m.PhysicalResistance : (int)(m.ArmorRating + 0.5)));
            Stream.Write((short)(Mobile.BodyWeight + m.TotalWeight));

            if (type >= 5)
            {
                Stream.Write((short)m.MaxWeight);
                Stream.Write((byte)(m.Race?.RaceID + 1 ?? 0)); // Would be 0x00 if it's a non-ML enabled account but...
            }

            Stream.Write((short)m.StatCap);

            Stream.Write((byte)m.Followers);
            Stream.Write((byte)m.FollowersMax);

            if (type >= 4)
            {
                Stream.Write((short)m.FireResistance);   // Fire
                Stream.Write((short)m.ColdResistance);   // Cold
                Stream.Write((short)m.PoisonResistance); // Poison
                Stream.Write((short)m.EnergyResistance); // Energy
                Stream.Write((short)m.Luck);             // Luck

                var weapon = m.Weapon;

                if (weapon != null)
                {
                    weapon.GetStatusDamage(m, out var min, out var max);
                    Stream.Write((short)min); // Damage min
                    Stream.Write((short)max); // Damage max
                }
                else
                {
                    Stream.Write((short)0); // Damage min
                    Stream.Write((short)0); // Damage max
                }

                Stream.Write(m.TithingPoints);
            }

            if (type >= 6)
            {
                for (var i = 0; i < 15; ++i)
                {
                    Stream.Write((short)m.GetAOSStatus(i));
                }
            }
        }
    }

    public sealed class MobileStatus : Packet
    {
        public MobileStatus(Mobile beholder, Mobile beheld) : this(beholder, beheld, beheld.NetState)
        {
        }

        public MobileStatus(Mobile beholder, Mobile beheld, NetState ns) : base(0x11)
        {
            var name = beheld.Name ?? "";

            int type;

            if (beholder != beheld)
            {
                type = 0;
                EnsureCapacity(43);
            }
            else if (Core.HS && ns?.ExtendedStatus == true)
            {
                type = 6;
                EnsureCapacity(121);
            }
            else if (Core.ML && ns?.SupportsExpansion(Expansion.ML) == true)
            {
                type = 5;
                EnsureCapacity(91);
            }
            else
            {
                type = Core.AOS ? 4 : 3;
                EnsureCapacity(88);
            }

            Stream.Write(beheld.Serial);

            Stream.WriteAsciiFixed(name, 30);

            if (beholder == beheld)
            {
                WriteAttr(beheld.Hits, beheld.HitsMax);
            }
            else
            {
                WriteAttrNorm(beheld.Hits, beheld.HitsMax);
            }

            Stream.Write(beheld.CanBeRenamedBy(beholder));

            Stream.Write((byte)type);

            if (type <= 0)
            {
                return;
            }

            Stream.Write(beheld.Female);

            Stream.Write((short)beheld.Str);
            Stream.Write((short)beheld.Dex);
            Stream.Write((short)beheld.Int);

            WriteAttr(beheld.Stam, beheld.StamMax);
            WriteAttr(beheld.Mana, beheld.ManaMax);

            Stream.Write(beheld.TotalGold);
            Stream.Write((short)(Core.AOS ? beheld.PhysicalResistance : (int)(beheld.ArmorRating + 0.5)));
            Stream.Write((short)(Mobile.BodyWeight + beheld.TotalWeight));

            if (type >= 5)
            {
                Stream.Write((short)beheld.MaxWeight);
                Stream.Write((byte)(beheld.Race.RaceID + 1)); // Would be 0x00 if it's a non-ML enabled account but...
            }

            Stream.Write((short)beheld.StatCap);

            Stream.Write((byte)beheld.Followers);
            Stream.Write((byte)beheld.FollowersMax);

            if (type >= 4)
            {
                Stream.Write((short)beheld.FireResistance);   // Fire
                Stream.Write((short)beheld.ColdResistance);   // Cold
                Stream.Write((short)beheld.PoisonResistance); // Poison
                Stream.Write((short)beheld.EnergyResistance); // Energy
                Stream.Write((short)beheld.Luck);             // Luck

                var weapon = beheld.Weapon;

                if (weapon != null)
                {
                    weapon.GetStatusDamage(beheld, out var min, out var max);
                    Stream.Write((short)min); // Damage min
                    Stream.Write((short)max); // Damage max
                }
                else
                {
                    Stream.Write((short)0); // Damage min
                    Stream.Write((short)0); // Damage max
                }

                Stream.Write(beheld.TithingPoints);
            }

            if (type >= 6)
            {
                for (var i = 0; i < 15; ++i)
                {
                    Stream.Write((short)beheld.GetAOSStatus(i));
                }
            }
        }

        private void WriteAttr(int current, int maximum)
        {
            Stream.Write((short)current);
            Stream.Write((short)maximum);
        }

        private void WriteAttrNorm(int current, int maximum)
        {
            AttributeNormalizer.WriteReverse(Stream, current, maximum);
        }
    }

    public sealed class MobileUpdate : Packet
    {
        public MobileUpdate(Mobile m, bool stygianAbyss) : base(0x20, 19)
        {
            var hue = m.Hue;

            if (m.SolidHueOverride >= 0)
            {
                hue = m.SolidHueOverride;
            }

            Stream.Write(m.Serial);
            Stream.Write((short)m.Body);
            Stream.Write((byte)0);
            Stream.Write((short)hue);
            Stream.Write((byte)m.GetPacketFlags(stygianAbyss));
            Stream.Write((short)m.X);
            Stream.Write((short)m.Y);
            Stream.Write((short)0);
            Stream.Write((byte)m.Direction);
            Stream.Write((sbyte)m.Z);
        }
    }

    public sealed class MobileIncoming : Packet
    {
        private static readonly ThreadLocal<int[]> m_DupedLayersTL = new(() => new int[256]);
        private static readonly ThreadLocal<int> m_VersionTL = new();

        public MobileIncoming(NetState ns, Mobile beholder, Mobile beheld) : base(0x78)
        {
            var sa = ns.StygianAbyss;
            var newPacket = ns.NewMobileIncoming;
            var itemIdMask = newPacket ? 0xFFFF : 0x7FFF;

            var m_Version = ++m_VersionTL.Value;
            var m_DupedLayers = m_DupedLayersTL.Value;

            var eq = beheld.Items;
            var count = eq.Count;

            if (beheld.HairItemID > 0)
            {
                count++;
            }

            if (beheld.FacialHairItemID > 0)
            {
                count++;
            }

            EnsureCapacity(23 + count * 9);

            var hue = beheld.Hue;

            if (beheld.SolidHueOverride >= 0)
            {
                hue = beheld.SolidHueOverride;
            }

            Stream.Write(beheld.Serial);
            Stream.Write((short)beheld.Body);
            Stream.Write((short)beheld.X);
            Stream.Write((short)beheld.Y);
            Stream.Write((sbyte)beheld.Z);
            Stream.Write((byte)beheld.Direction);
            Stream.Write((short)hue);
            Stream.Write((byte)beheld.GetPacketFlags(sa));
            Stream.Write((byte)Notoriety.Compute(beholder, beheld));

            for (var i = 0; i < eq.Count; ++i)
            {
                var item = eq[i];

                var layer = (byte)item.Layer;

                if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers![layer] != m_Version)
                {
                    m_DupedLayers[layer] = m_Version;

                    hue = item.Hue;

                    if (beheld.SolidHueOverride >= 0)
                    {
                        hue = beheld.SolidHueOverride;
                    }

                    var itemID = item.ItemID & itemIdMask;
                    var writeHue = newPacket || hue != 0;

                    if (!newPacket && writeHue)
                    {
                        itemID |= 0x8000;
                    }

                    Stream.Write(item.Serial);
                    Stream.Write((ushort)itemID);
                    Stream.Write(layer);

                    if (writeHue)
                    {
                        Stream.Write((short)hue);
                    }
                }
            }

            if (beheld.HairItemID > 0)
            {
                if (m_DupedLayers![(int)Layer.Hair] != m_Version)
                {
                    m_DupedLayers[(int)Layer.Hair] = m_Version;
                    hue = beheld.HairHue;

                    if (beheld.SolidHueOverride >= 0)
                    {
                        hue = beheld.SolidHueOverride;
                    }

                    var itemID = beheld.HairItemID & itemIdMask;
                    var writeHue = newPacket || hue != 0;

                    if (!newPacket && writeHue)
                    {
                        itemID |= 0x8000;
                    }

                    Stream.Write(HairInfo.FakeSerial(beheld.Serial));
                    Stream.Write((ushort)itemID);
                    Stream.Write((byte)Layer.Hair);

                    if (writeHue)
                    {
                        Stream.Write((short)hue);
                    }
                }
            }

            if (beheld.FacialHairItemID > 0)
            {
                if (m_DupedLayers![(int)Layer.FacialHair] != m_Version)
                {
                    m_DupedLayers[(int)Layer.FacialHair] = m_Version;
                    hue = beheld.FacialHairHue;

                    if (beheld.SolidHueOverride >= 0)
                    {
                        hue = beheld.SolidHueOverride;
                    }

                    var itemID = beheld.FacialHairItemID & itemIdMask;
                    var writeHue = newPacket || hue != 0;

                    if (!newPacket && writeHue)
                    {
                        itemID |= 0x8000;
                    }

                    Stream.Write(FacialHairInfo.FakeSerial(beheld.Serial));
                    Stream.Write((ushort)itemID);
                    Stream.Write((byte)Layer.FacialHair);

                    if (writeHue)
                    {
                        Stream.Write((short)hue);
                    }
                }
            }

            Stream.Write(0); // terminate
        }
    }

    public sealed class RemoveEntity : Packet
    {
        public RemoveEntity(Serial entity) : base(0x1D, 5)
        {
            Stream.Write(entity);
        }
    }
}
