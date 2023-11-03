using System.IO;
using Server;
using Server.Collections;
using Server.Items;
using Server.Multis;
using Server.Network;

namespace UOContent.Tests
{
    public sealed class MoveBoatHS : Packet
    {
        public MoveBoatHS(
            Mobile beholder, BaseBoat boat, Direction d,
            int speed, PooledRefList<IEntity> ents, int xOffset,
            int yOffset
        ) : base(0xF6)
        {
            EnsureCapacity(3 + 15 + ents.Count * 10);

            Stream.Write(boat.Serial);
            Stream.Write((byte)speed);
            Stream.Write((byte)d);
            Stream.Write((byte)boat.Facing);
            Stream.Write((short)(boat.X + xOffset));
            Stream.Write((short)(boat.Y + yOffset));
            Stream.Write((short)boat.Z);
            Stream.Write((short)0); // count placeholder

            var count = 0;

            foreach (var ent in ents)
            {
                Stream.Write(ent.Serial);
                Stream.Write((short)(ent.X + xOffset));
                Stream.Write((short)(ent.Y + yOffset));
                Stream.Write((short)ent.Z);
                ++count;
            }

            Stream.Seek(16, SeekOrigin.Begin);
            Stream.Write((short)count);
        }
    }

    public sealed class DisplayBoatHS : Packet
    {
        public DisplayBoatHS(Mobile beholder, BaseBoat boat) : base(0xF7)
        {
            var ents = boat.GetMovingEntities(true);

            EnsureCapacity(3 + 2 + 5 * 26);

            Stream.Write((short)0); // count placeholder

            var count = 0;

            foreach (var ent in ents)
            {
                if (!beholder.CanSee(ent))
                {
                    continue;
                }

                // Embedded WorldItemHS packets
                Stream.Write((byte)0xF3);
                Stream.Write((short)0x1);

                if (ent is BaseMulti bm)
                {
                    Stream.Write((byte)0x02);
                    Stream.Write(bm.Serial);
                    // TODO: Mask no longer needed, merge with Item case?
                    Stream.Write((ushort)(bm.ItemID & 0x3FFF));
                    Stream.Write((byte)0);

                    Stream.Write((short)bm.Amount);
                    Stream.Write((short)bm.Amount);

                    Stream.Write((short)(bm.X & 0x7FFF));
                    Stream.Write((short)(bm.Y & 0x3FFF));
                    Stream.Write((sbyte)bm.Z);

                    Stream.Write((byte)bm.Light);
                    Stream.Write((short)bm.Hue);
                    Stream.Write((byte)bm.GetPacketFlags());
                }
                else if (ent is Mobile m)
                {
                    Stream.Write((byte)0x01);
                    Stream.Write(m.Serial);
                    Stream.Write((short)m.Body);
                    Stream.Write((byte)0);

                    Stream.Write((short)1);
                    Stream.Write((short)1);

                    Stream.Write((short)(m.X & 0x7FFF));
                    Stream.Write((short)(m.Y & 0x3FFF));
                    Stream.Write((sbyte)m.Z);

                    Stream.Write((byte)m.Direction);
                    Stream.Write((short)m.Hue);
                    Stream.Write((byte)m.GetPacketFlags(true));
                }
                else if (ent is Item item)
                {
                    Stream.Write((byte)0x00);
                    Stream.Write(item.Serial);
                    Stream.Write((ushort)(item.ItemID & 0xFFFF));
                    Stream.Write((byte)0);

                    Stream.Write((short)item.Amount);
                    Stream.Write((short)item.Amount);

                    Stream.Write((short)(item.X & 0x7FFF));
                    Stream.Write((short)(item.Y & 0x3FFF));
                    Stream.Write((sbyte)item.Z);

                    Stream.Write((byte)item.Light);
                    Stream.Write((short)item.Hue);
                    Stream.Write((byte)item.GetPacketFlags());
                }

                Stream.Write((short)0x00);
                ++count;
            }

            Stream.Seek(3, SeekOrigin.Begin);
            Stream.Write((short)count);
        }
    }
}
