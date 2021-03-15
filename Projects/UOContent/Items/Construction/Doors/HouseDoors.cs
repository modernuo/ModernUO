using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;

namespace Server.Items
{
    public class MetalHouseDoor : BaseHouseDoor
    {
        [Constructible]
        public MetalHouseDoor(DoorFacing facing) : base(
            facing,
            0x675 + 2 * (int)facing,
            0x676 + 2 * (int)facing,
            0xEC,
            0xF3,
            GetOffset(facing)
        )
        {
        }

        public MetalHouseDoor(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DarkWoodHouseDoor : BaseHouseDoor
    {
        [Constructible]
        public DarkWoodHouseDoor(DoorFacing facing) : base(
            facing,
            0x6A5 + 2 * (int)facing,
            0x6A6 + 2 * (int)facing,
            0xEA,
            0xF1,
            GetOffset(facing)
        )
        {
        }

        public DarkWoodHouseDoor(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class GenericHouseDoor : BaseHouseDoor
    {
        [Constructible]
        public GenericHouseDoor(DoorFacing facing, int baseItemID, int openedSound, int closedSound, bool autoAdjust = true)
            : base(
                facing,
                baseItemID + (autoAdjust ? 2 * (int)facing : 0),
                baseItemID + 1 + (autoAdjust ? 2 * (int)facing : 0),
                openedSound,
                closedSound,
                GetOffset(facing)
            )
        {
        }

        public GenericHouseDoor(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public abstract class BaseHouseDoor : BaseDoor, ISecurable
    {
        public BaseHouseDoor(DoorFacing facing, int closedID, int openedID, int openedSound, int closedSound, Point3D offset)
            : base(closedID, openedID, openedSound, closedSound, offset)
        {
            Facing = facing;
            Level = SecureLevel.Anyone;
        }

        public BaseHouseDoor(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DoorFacing Facing { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public BaseHouse FindHouse()
        {
            Point3D loc;

            if (Open)
            {
                loc = new Point3D(X - Offset.X, Y - Offset.Y, Z - Offset.Z);
            }
            else
            {
                loc = Location;
            }

            return BaseHouse.FindHouseAt(loc, Map, 20);
        }

        public bool CheckAccess(Mobile m)
        {
            var house = FindHouse();

            if (house == null)
            {
                return false;
            }

            if (!house.IsAosRules)
            {
                return true;
            }

            if (house.Public ? house.IsBanned(m) : !house.HasAccess(m))
            {
                return false;
            }

            return house.HasSecureAccess(m, Level);
        }

        public override void OnOpened(Mobile from)
        {
            var house = FindHouse();

            if (house?.IsFriend(from) == true && from.AccessLevel == AccessLevel.Player && house.RefreshDecay())
            {
                from.SendLocalizedMessage(1043293); // Your house's age and contents have been refreshed.
            }

            if (house?.Public == true && !house.IsFriend(from))
            {
                house.Visits++;
            }
        }

        public override bool UseLocks() => FindHouse()?.IsAosRules != true;

        public override void Use(Mobile from)
        {
            if (!CheckAccess(from))
            {
                from.SendLocalizedMessage(1061637); // You are not allowed to access this.
            }
            else
            {
                base.Use(from);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write((int)Level);

            writer.Write((int)Facing);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Level = (SecureLevel)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 1)
                        {
                            Level = SecureLevel.Anyone;
                        }

                        Facing = (DoorFacing)reader.ReadInt();
                        break;
                    }
            }
        }

        public override bool IsInside(Mobile from)
        {
            int x, y, w, h;

            const int r = 2;
            const int bs = r * 2 + 1;
            const int ss = r + 1;

            switch (Facing)
            {
                case DoorFacing.WestCW:
                case DoorFacing.EastCCW:
                    x = -r;
                    y = -r;
                    w = bs;
                    h = ss;
                    break;

                case DoorFacing.EastCW:
                case DoorFacing.WestCCW:
                    x = -r;
                    y = 0;
                    w = bs;
                    h = ss;
                    break;

                case DoorFacing.SouthCW:
                case DoorFacing.NorthCCW:
                    x = -r;
                    y = -r;
                    w = ss;
                    h = bs;
                    break;

                case DoorFacing.NorthCW:
                case DoorFacing.SouthCCW:
                    x = 0;
                    y = -r;
                    w = ss;
                    h = bs;
                    break;

                // No way to test the 'insideness' of SE Sliding doors on OSI, so leaving them default to false until further information gained

                default: return false;
            }

            var rx = from.X - X;
            var ry = from.Y - Y;
            var az = (from.Z - Z).Abs();

            return rx >= x && rx < x + w && ry >= y && ry < y + h && az <= 4;
        }
    }
}
