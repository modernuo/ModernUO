using System.Buffers;
using System.IO;
using Server.Items;

namespace Server.Network
{
    public static class CorpsePackets
    {
        public static void SendCorpseEquip(this NetState ns, Mobile beholder, Corpse beheld)
        {
            if (ns == null)
            {
                return;
            }

            var list = beheld.EquipItems;

            var maxLength = 8 + (list.Count + 2) * 5;
            var writer = new SpanWriter(stackalloc byte[maxLength]);
            writer.Write((byte)0x89);
            writer.Seek(2, SeekOrigin.Current);
            writer.Write(beheld.Serial);

            for (var i = 0; i < list.Count; ++i)
            {
                var item = list[i];

                if (!item.Deleted && beholder.CanSee(item) && item.Parent == beheld)
                {
                    writer.Write((byte)(item.Layer + 1));
                    writer.Write(item.Serial);
                }
            }

            if (beheld.Hair?.ItemID > 0)
            {
                writer.Write((byte)(Layer.Hair + 1));
                writer.Write(HairInfo.FakeSerial(beheld.Owner.Serial) - 2);
            }

            if (beheld.FacialHair?.ItemID > 0)
            {
                writer.Write((byte)(Layer.FacialHair + 1));
                writer.Write(FacialHairInfo.FakeSerial(beheld.Owner.Serial) - 2);
            }

            writer.Write((byte)Layer.Invalid);

            writer.WritePacketLength();
            ns.Send(writer.Span);
        }

        public static void SendCorpseContent(this NetState ns, Mobile beholder, Corpse beheld)
        {
            if (ns == null)
            {
                return;
            }

            var list = beheld.EquipItems;

            var maxLength = 5 + (list.Count + 2) * (ns.ContainerGridLines ? 19 : 20);
            var writer = new SpanWriter(stackalloc byte[maxLength]);
            writer.Write((byte)0x3C);
            writer.Seek(4, SeekOrigin.Current); // Length and Count

            var written = 0;
            for (var i = 0; i < list.Count; ++i)
            {
                var child = list[i];

                if (!child.Deleted && child.Parent == beheld && beholder.CanSee(child))
                {
                    writer.Write(child.Serial);
                    writer.Write((ushort)child.ItemID);
                    writer.Write((byte)0); // signed, itemID offset
                    writer.Write((ushort)child.Amount);
                    writer.Write((short)child.X);
                    writer.Write((short)child.Y);
                    if (ns.ContainerGridLines)
                    {
                        writer.Write((byte)0); // Grid Location?
                    }
                    writer.Write(beheld.Serial);
                    writer.Write((ushort)child.Hue);

                    ++written;
                }
            }

            if (beheld.Hair?.ItemID > 0)
            {
                writer.Write(HairInfo.FakeSerial(beheld.Owner.Serial) - 2);
                writer.Write((ushort)beheld.Hair.ItemID);
                writer.Write((byte)0); // signed, itemID offset
                writer.Write((ushort)1);
                writer.Write(0); // X/Y
                if (ns.ContainerGridLines)
                {
                    writer.Write((byte)0); // Grid Location?
                }
                writer.Write(beheld.Serial);
                writer.Write((ushort)beheld.Hair.Hue);

                ++written;
            }

            if (beheld.FacialHair?.ItemID > 0)
            {
                writer.Write(FacialHairInfo.FakeSerial(beheld.Owner.Serial) - 2);
                writer.Write((ushort)beheld.FacialHair.ItemID);
                writer.Write((byte)0); // signed, itemID offset
                writer.Write((ushort)1);
                writer.Write(0); // X/Y
                if (ns.ContainerGridLines)
                {
                    writer.Write((byte)0); // Grid Location?
                }
                writer.Write(beheld.Serial);
                writer.Write((ushort)beheld.FacialHair.Hue);

                ++written;
            }

            writer.Seek(1, SeekOrigin.Begin);
            writer.Write((ushort)writer.BytesWritten);
            writer.Write((ushort)written);
            writer.Seek(0, SeekOrigin.End);
            ns.Send(writer.Span);
        }
    }
}
