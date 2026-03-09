using System.Collections.Generic;

namespace Server.Network;

public sealed class VendorSellList : Packet
{
    public VendorSellList(Mobile shopkeeper, List<SellItemState> sis) : base(0x9E)
    {
        EnsureCapacity(256);

        Stream.Write(shopkeeper.Serial);

        Stream.Write((ushort)sis.Count);

        foreach (var state in sis)
        {
            Stream.Write(state.Item.Serial);
            Stream.Write((ushort)state.Item.ItemID);
            Stream.Write((ushort)state.Item.Hue);
            Stream.Write((ushort)state.Item.Amount);
            Stream.Write((ushort)state.Price);

            var name = string.IsNullOrWhiteSpace(state.Item.Name) ? state.Name ?? "" : state.Item.Name.Trim();

            Stream.Write((ushort)name.Length);
            Stream.WriteAsciiFixed(name, (ushort)name.Length);
        }
    }
}

public sealed class EndVendorSell : Packet
{
    public EndVendorSell(Mobile vendor) : base(0x3B, 8)
    {
        Stream.Write((ushort)8); // length
        Stream.Write(vendor.Serial);
        Stream.Write((byte)0);
    }
}
