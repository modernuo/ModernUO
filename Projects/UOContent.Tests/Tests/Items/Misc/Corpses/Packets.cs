using System.IO;
using Server.Items;

namespace Server.Network;

public sealed class CorpseEquip : Packet
{
    public CorpseEquip(Mobile beholder, Corpse beheld) : base(0x89)
    {
        var list = beheld.EquipItems;

        var count = list.Count;
        var hair = beheld.Hair;
        var facialHair = beheld.FacialHair;

        if (hair != null)
        {
            count++;
        }

        if (facialHair != null)
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

        if (hair?.ItemId > 0)
        {
            Stream.Write((byte)(Layer.Hair + 1));
            Stream.Write(hair.VirtualSerial);
        }

        if (facialHair?.ItemId > 0)
        {
            Stream.Write((byte)(Layer.FacialHair + 1));
            Stream.Write(facialHair.VirtualSerial);
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
        var hair = beheld.Hair;
        var facialHair = beheld.FacialHair;

        if (hair != null)
        {
            count++;
        }

        if (facialHair != null)
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

        if (hair?.ItemId > 0)
        {
            Stream.Write(hair.VirtualSerial);
            Stream.Write((ushort)hair.ItemId);
            Stream.Write((byte)0); // signed, itemID offset
            Stream.Write((ushort)1);
            Stream.Write((short)0);
            Stream.Write((short)0);
            Stream.Write(beheld.Serial);
            Stream.Write((ushort)hair.Hue);

            ++written;
        }

        if (facialHair?.ItemId > 0)
        {
            Stream.Write(facialHair.VirtualSerial);
            Stream.Write((ushort)facialHair.ItemId);
            Stream.Write((byte)0); // signed, itemID offset
            Stream.Write((ushort)1);
            Stream.Write((short)0);
            Stream.Write((short)0);
            Stream.Write(beheld.Serial);
            Stream.Write((ushort)facialHair.Hue);

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
        var hair = beheld.Hair;
        var facialHair = beheld.FacialHair;

        if (hair != null)
        {
            count++;
        }

        if (facialHair != null)
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

        if (hair?.ItemId > 0)
        {
            Stream.Write(hair.VirtualSerial);
            Stream.Write((ushort)hair.ItemId);
            Stream.Write((byte)0); // signed, itemID offset
            Stream.Write((ushort)1);
            Stream.Write((short)0);
            Stream.Write((short)0);
            Stream.Write((byte)0); // Grid Location?
            Stream.Write(beheld.Serial);
            Stream.Write((ushort)hair.Hue);

            ++written;
        }

        if (facialHair?.ItemId > 0)
        {
            Stream.Write(facialHair.VirtualSerial);
            Stream.Write((ushort)facialHair.ItemId);
            Stream.Write((byte)0); // signed, itemID offset
            Stream.Write((ushort)1);
            Stream.Write((short)0);
            Stream.Write((short)0);
            Stream.Write((byte)0); // Grid Location?
            Stream.Write(beheld.Serial);
            Stream.Write((ushort)facialHair.Hue);

            ++written;
        }

        Stream.Seek(pos, SeekOrigin.Begin);
        Stream.Write((ushort)written);
    }
}
