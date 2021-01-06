using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server
{
    public static class OutgoingVirtualHairPackets
    {
        public const int EquipUpdatePacketLength = 15;
        public const int RemovePacketLength = 5;

        public static void SendVirtualHairEquipUpdatePacket(this NetState ns, Mobile m, Serial hairSerial, Layer layer)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[EquipUpdatePacketLength];
            CreateVirtualHairEquipUpdatePacket(buffer, m, hairSerial, layer);
            ns.Send(buffer);
        }

        public static void CreateVirtualHairEquipUpdatePacket(Span<byte> buffer, Mobile m, Serial hairSerial, Layer layer)
        {
            var hue = m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.HairHue;

            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x2E); // Packet ID

            writer.Write(hairSerial);
            writer.Write((short)m.HairItemID);
            writer.Write((byte)0);
            writer.Write((byte)layer);
            writer.Write(m.Serial);
            writer.Write((short)hue);
        }

        public static void SendRemoveVirtualHairPacket(this NetState ns, Serial hairSerial)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[RemovePacketLength];
            CreateVirtualRemoveHairPacket(buffer, hairSerial);
            ns.Send(buffer);
        }

        public static void CreateVirtualRemoveHairPacket(Span<byte> buffer, Serial hairSerial)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x1D); // Packet ID
            writer.Write(hairSerial);
        }
    }

    public abstract class BaseHairInfo
    {
        protected BaseHairInfo(int itemid, int hue = 0)
        {
            ItemID = itemid;
            Hue = hue;
        }

        protected BaseHairInfo(IGenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        ItemID = reader.ReadInt();
                        Hue = reader.ReadInt();
                        break;
                    }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ItemID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Hue { get; set; }

        public virtual void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version
            writer.Write(ItemID);
            writer.Write(Hue);
        }
    }

    public class HairInfo : BaseHairInfo
    {
        public HairInfo(int itemid)
            : base(itemid)
        {
        }

        public HairInfo(int itemid, int hue)
            : base(itemid, hue)
        {
        }

        public HairInfo(IGenericReader reader)
            : base(reader)
        {
        }

        // TODO: Can we make this higher for newer clients?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FakeSerial(Serial m) => 0x7FFFFFFF - 0x400 - m * 4;
    }

    public class FacialHairInfo : BaseHairInfo
    {
        public FacialHairInfo(int itemid)
            : base(itemid)
        {
        }

        public FacialHairInfo(int itemid, int hue)
            : base(itemid, hue)
        {
        }

        public FacialHairInfo(IGenericReader reader)
            : base(reader)
        {
        }

        // TODO: Can we make this higher for newer clients?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FakeSerial(Serial m) => 0x7FFFFFFF - 0x400 - 1 - m * 4;
    }
}
