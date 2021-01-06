using System.IO;
using Server.Items;

namespace Server.Network
{
    public sealed class CorpseEquip : Packet
    {
        public CorpseEquip(Mobile beholder, Corpse beheld) : base(0x89)
        {
            var list = beheld.EquipItems;

            var count = list.Count;
            if (beheld.Hair?.ItemID > 0)
            {
                count++;
            }

            if (beheld.FacialHair?.ItemID > 0)
            {
                count++;
            }

            EnsureCapacity(8 + count * 5);

            Stream.Write(beheld.Serial);

            for (var i = 0; i < list.Count; ++i)
            {
                var item = list[i];

                if (!item.Deleted && beholder.CanSee(item) && item.Parent == beheld)
                {
                    Stream.Write((byte)(item.Layer + 1));
                    Stream.Write(item.Serial);
                }
            }

            if (beheld.Hair?.ItemID > 0)
            {
                Stream.Write((byte)(Layer.Hair + 1));
                Stream.Write(HairInfo.FakeSerial(beheld.Owner.Serial) - 2);
            }

            if (beheld.FacialHair?.ItemID > 0)
            {
                Stream.Write((byte)(Layer.FacialHair + 1));
                Stream.Write(FacialHairInfo.FakeSerial(beheld.Owner.Serial) - 2);
            }

            Stream.Write((byte)Layer.Invalid);
        }
    }

    public sealed class CorpseContent : Packet
    {
        public CorpseContent(Mobile beholder, Corpse beheld)
            : base(0x3C)
        {
            var items = beheld.EquipItems;
            var count = items.Count;

            if (beheld.Hair?.ItemID > 0)
            {
                count++;
            }

            if (beheld.FacialHair?.ItemID > 0)
            {
                count++;
            }

            EnsureCapacity(5 + count * 19);

            var pos = Stream.Position;

            var written = 0;

            Stream.Write((ushort)0);

            for (var i = 0; i < items.Count; ++i)
            {
                var child = items[i];

                if (!child.Deleted && child.Parent == beheld && beholder.CanSee(child))
                {
                    Stream.Write(child.Serial);
                    Stream.Write((ushort)child.ItemID);
                    Stream.Write((byte)0); // signed, itemID offset
                    Stream.Write((ushort)child.Amount);
                    Stream.Write((short)child.X);
                    Stream.Write((short)child.Y);
                    Stream.Write(beheld.Serial);
                    Stream.Write((ushort)child.Hue);

                    ++written;
                }
            }

            if (beheld.Hair?.ItemID > 0)
            {
                Stream.Write(HairInfo.FakeSerial(beheld.Owner.Serial) - 2);
                Stream.Write((ushort)beheld.Hair.ItemID);
                Stream.Write((byte)0); // signed, itemID offset
                Stream.Write((ushort)1);
                Stream.Write((short)0);
                Stream.Write((short)0);
                Stream.Write(beheld.Serial);
                Stream.Write((ushort)beheld.Hair.Hue);

                ++written;
            }

            if (beheld.FacialHair?.ItemID > 0)
            {
                Stream.Write(FacialHairInfo.FakeSerial(beheld.Owner.Serial) - 2);
                Stream.Write((ushort)beheld.FacialHair.ItemID);
                Stream.Write((byte)0); // signed, itemID offset
                Stream.Write((ushort)1);
                Stream.Write((short)0);
                Stream.Write((short)0);
                Stream.Write(beheld.Serial);
                Stream.Write((ushort)beheld.FacialHair.Hue);

                ++written;
            }

            Stream.Seek(pos, SeekOrigin.Begin);
            Stream.Write((ushort)written);
        }
    }

    public sealed class CorpseContent6017 : Packet
    {
        public CorpseContent6017(Mobile beholder, Corpse beheld)
            : base(0x3C)
        {
            var items = beheld.EquipItems;
            var count = items.Count;

            if (beheld.Hair?.ItemID > 0)
            {
                count++;
            }

            if (beheld.FacialHair?.ItemID > 0)
            {
                count++;
            }

            EnsureCapacity(5 + count * 20);

            var pos = Stream.Position;

            var written = 0;

            Stream.Write((ushort)0);

            for (var i = 0; i < items.Count; ++i)
            {
                var child = items[i];

                if (!child.Deleted && child.Parent == beheld && beholder.CanSee(child))
                {
                    Stream.Write(child.Serial);
                    Stream.Write((ushort)child.ItemID);
                    Stream.Write((byte)0); // signed, itemID offset
                    Stream.Write((ushort)child.Amount);
                    Stream.Write((short)child.X);
                    Stream.Write((short)child.Y);
                    Stream.Write((byte)0); // Grid Location?
                    Stream.Write(beheld.Serial);
                    Stream.Write((ushort)child.Hue);

                    ++written;
                }
            }

            if (beheld.Hair?.ItemID > 0)
            {
                Stream.Write(HairInfo.FakeSerial(beheld.Owner.Serial) - 2);
                Stream.Write((ushort)beheld.Hair.ItemID);
                Stream.Write((byte)0); // signed, itemID offset
                Stream.Write((ushort)1);
                Stream.Write((short)0);
                Stream.Write((short)0);
                Stream.Write((byte)0); // Grid Location?
                Stream.Write(beheld.Serial);
                Stream.Write((ushort)beheld.Hair.Hue);

                ++written;
            }

            if (beheld.FacialHair?.ItemID > 0)
            {
                Stream.Write(FacialHairInfo.FakeSerial(beheld.Owner.Serial) - 2);
                Stream.Write((ushort)beheld.FacialHair.ItemID);
                Stream.Write((byte)0); // signed, itemID offset
                Stream.Write((ushort)1);
                Stream.Write((short)0);
                Stream.Write((short)0);
                Stream.Write((byte)0); // Grid Location?
                Stream.Write(beheld.Serial);
                Stream.Write((ushort)beheld.FacialHair.Hue);

                ++written;
            }

            Stream.Seek(pos, SeekOrigin.Begin);
            Stream.Write((ushort)written);
        }
    }
}
