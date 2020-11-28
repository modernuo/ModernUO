using Server.Items;

namespace Server.Network
{
    public sealed class WorldItem : Packet
    {
        public WorldItem(Item item) : base(0x1A)
        {
            EnsureCapacity(20);

            // 14 base length
            // +2 - Amount
            // +2 - Hue
            // +1 - Flags

            var serial = item.Serial.Value;
            var itemID = item.ItemID & 0x3FFF;
            var amount = item.Amount;
            var loc = item.Location;
            var x = loc.X;
            var y = loc.Y;
            var hue = item.Hue;
            var flags = item.GetPacketFlags();
            var direction = (int)item.Direction;

            if (amount != 0)
            {
                serial |= 0x80000000;
            }
            else
            {
                serial &= 0x7FFFFFFF;
            }

            Stream.Write(serial);

            if (item is BaseMulti)
            {
                Stream.Write((short)(itemID | 0x4000));
            }
            else
            {
                Stream.Write((short)itemID);
            }

            if (amount != 0)
            {
                Stream.Write((short)amount);
            }

            x &= 0x7FFF;

            if (direction != 0)
            {
                x |= 0x8000;
            }

            Stream.Write((short)x);

            y &= 0x3FFF;

            if (hue != 0)
            {
                y |= 0x8000;
            }

            if (flags != 0)
            {
                y |= 0x4000;
            }

            Stream.Write((short)y);

            if (direction != 0)
            {
                Stream.Write((byte)direction);
            }

            Stream.Write((sbyte)loc.Z);

            if (hue != 0)
            {
                Stream.Write((ushort)hue);
            }

            if (flags != 0)
            {
                Stream.Write((byte)flags);
            }
        }
    }

    public sealed class WorldItemSA : Packet
    {
        public WorldItemSA(Item item) : base(0xF3, 24)
        {
            Stream.Write((short)0x1);

            var itemID = item.ItemID;

            if (item is BaseMulti)
            {
                Stream.Write((byte)0x02);

                Stream.Write(item.Serial);

                itemID &= 0x3FFF;

                Stream.Write((short)itemID);

                Stream.Write((byte)0);
            }
            else
            {
                Stream.Write((byte)0x00);

                Stream.Write(item.Serial);

                itemID &= 0x7FFF;

                Stream.Write((short)itemID);

                Stream.Write((byte)0);
            }

            var amount = item.Amount;
            Stream.Write((short)amount);
            Stream.Write((short)amount);

            var loc = item.Location;
            Stream.Write((short)loc.X);
            Stream.Write((short)loc.Y);
            Stream.Write((sbyte)loc.Z);

            Stream.Write((byte)item.Light);
            Stream.Write((short)item.Hue);
            Stream.Write((byte)item.GetPacketFlags());
        }
    }

    public sealed class WorldItemHS : Packet
    {
        public WorldItemHS(Item item) : base(0xF3, 26)
        {
            Stream.Write((short)0x1);

            var itemID = item.ItemID;

            if (item is BaseMulti)
            {
                Stream.Write((byte)0x02);

                Stream.Write(item.Serial);

                itemID &= 0x3FFF;

                Stream.Write((ushort)itemID);

                Stream.Write((byte)0);
            }
            else
            {
                Stream.Write((byte)0x00);

                Stream.Write(item.Serial);

                itemID &= 0xFFFF;

                Stream.Write((ushort)itemID);

                Stream.Write((byte)0);
            }

            var amount = item.Amount;
            Stream.Write((short)amount);
            Stream.Write((short)amount);

            var loc = item.Location;
            Stream.Write((short)loc.X);
            Stream.Write((short)loc.Y);
            Stream.Write((sbyte)loc.Z);

            Stream.Write((byte)item.Light);
            Stream.Write((short)item.Hue);
            Stream.Write((byte)item.GetPacketFlags());

            Stream.Write((short)0x00); // ??
        }
    }

    public sealed class OPLInfo : Packet
    {
        /*public OPLInfo( ObjectPropertyList list ) : base( 0xBF )
        {
          EnsureCapacity( 13 );

          m_Stream.Write( (short) 0x10 );
          m_Stream.Write( (int) list.Entity.Serial );
          m_Stream.Write( (int) list.Hash );
        }*/

        public OPLInfo(Serial serial, int hash) : base(0xDC, 9)
        {
            Stream.Write(serial);
            Stream.Write(hash);
        }
    }
}
