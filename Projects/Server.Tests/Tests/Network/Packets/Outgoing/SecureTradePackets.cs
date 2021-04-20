using Server.Items;

namespace Server.Network
{
    public sealed class DisplaySecureTrade : Packet
    {
        public DisplaySecureTrade(Mobile them, Container first, Container second, string name)
            : base(0x6F)
        {
            name ??= "";

            EnsureCapacity(17 + name.Length);

            Stream.Write((byte)0); // Display
            Stream.Write(them.Serial);
            Stream.Write(first.Serial);
            Stream.Write(second.Serial);
            Stream.Write(true);

            Stream.WriteAsciiFixed(name, 30);
        }
    }

    public sealed class CloseSecureTrade : Packet
    {
        public CloseSecureTrade(Container cont)
            : base(0x6F)
        {
            EnsureCapacity(17);

            Stream.Write((byte)1); // Close
            Stream.Write(cont.Serial);
            Stream.Write(0);
            Stream.Write(0);
            Stream.Write(false);
        }
    }

    public sealed class UpdateSecureTrade : Packet
    {
        public UpdateSecureTrade(Container cont, bool first, bool second)
            : this(cont, TradeFlag.Update, first ? 1 : 0, second ? 1 : 0)
        {
        }

        public UpdateSecureTrade(Container cont, TradeFlag flag, int first, int second)
            : base(0x6F)
        {
            EnsureCapacity(17);

            Stream.Write((byte)flag);
            Stream.Write(cont.Serial);
            Stream.Write(first);
            Stream.Write(second);
            Stream.Write(false);
        }
    }

    public sealed class SecureTradeEquip : Packet
    {
        public SecureTradeEquip(Item item, Mobile m) : base(0x25, 20)
        {
            Stream.Write(item.Serial);
            Stream.Write((short)item.ItemID);
            Stream.Write((byte)0);
            Stream.Write((short)item.Amount);
            Stream.Write((short)item.X);
            Stream.Write((short)item.Y);
            Stream.Write(m.Serial);
            Stream.Write((short)item.Hue);
        }
    }

    public sealed class SecureTradeEquip6017 : Packet
    {
        public SecureTradeEquip6017(Item item, Mobile m) : base(0x25, 21)
        {
            Stream.Write(item.Serial);
            Stream.Write((short)item.ItemID);
            Stream.Write((byte)0);
            Stream.Write((short)item.Amount);
            Stream.Write((short)item.X);
            Stream.Write((short)item.Y);
            Stream.Write((byte)0); // Grid Location?
            Stream.Write(m.Serial);
            Stream.Write((short)item.Hue);
        }
    }
}
