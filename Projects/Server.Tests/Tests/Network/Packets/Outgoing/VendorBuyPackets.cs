using System.Collections.Generic;
using Server.Items;

namespace Server.Network
{
    public sealed class VendorBuyContent : Packet
    {
        public VendorBuyContent(List<BuyItemState> list, bool containerGridLines) : base(0x3C)
        {
            EnsureCapacity(list.Count * 19 + 5);

            Stream.Write((short)list.Count);

            for (var i = list.Count - 1; i >= 0; --i)
            {
                var bis = list[i];

                Stream.Write(bis.MySerial);
                Stream.Write((ushort)bis.ItemID);
                Stream.Write((byte)0); // itemID offset
                Stream.Write((ushort)bis.Amount);
                Stream.Write((short)(i + 1)); // x
                Stream.Write((short)1);       // y
                if (containerGridLines)
                {
                    Stream.Write((byte)0); // Grid Location?
                }
                Stream.Write(bis.ContainerSerial);
                Stream.Write((ushort)bis.Hue);
            }
        }
    }

    public sealed class DisplayBuyList : Packet
    {
        public DisplayBuyList(Serial vendor, bool highSeas) : base(0x24, highSeas ? 9 : 7)
        {
            Stream.Write(vendor);
            Stream.Write((short)0x30); // buy window id?
            if (highSeas)
            {
                Stream.Write((short)0x00);
            }
        }
    }

    public sealed class VendorBuyList : Packet
    {
        public VendorBuyList(Mobile vendor, List<BuyItemState> list) : base(0x74)
        {
            EnsureCapacity(256);

            Stream.Write(!(vendor.FindItemOnLayer(Layer.ShopBuy) is Container buyPack) ? Serial.MinusOne : buyPack.Serial);

            Stream.Write((byte)list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                var bis = list[i];

                Stream.Write(bis.Price);

                var desc = bis.Description ?? "";

                Stream.Write((byte)(desc.Length + 1));
                Stream.WriteAsciiNull(desc);
            }
        }
    }

    public sealed class EndVendorBuy : Packet
    {
        public EndVendorBuy(Serial vendor) : base(0x3B, 8)
        {
            Stream.Write((ushort)8); // length
            Stream.Write(vendor);
            Stream.Write((byte)0);
        }
    }
}
