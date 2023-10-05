using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.Multis
{
    public enum FoundationType
    {
        Stone,
        DarkWood,
        LightWood,
        Dungeon,
        Brick,
        ElvenGrey,
        ElvenNatural,
        Crystal,
        Shadow
    }

    public class HouseFoundation : BaseHouse
    {
        private static ComponentVerification m_Verification;

        public static readonly bool AllowStairSectioning = true;

        /* Stair block IDs
         * (sorted ascending)
         */
        private static readonly int[] m_BlockIDs =
        {
            0x3EE, 0x709, 0x71E, 0x721,
            0x738, 0x750, 0x76C, 0x788,
            0x7A3, 0x7BA, 0x35D2, 0x3609,
            0x4317, 0x4318, 0x4B07, 0x7807
        };

        /* Stair sequence IDs
         * (sorted ascending)
         * Use this for stairs in the proper N,W,S,E sequence
         */
        private static readonly int[] m_StairSeqs =
        {
            0x3EF, 0x70A, 0x722, 0x739,
            0x751, 0x76D, 0x789, 0x7A4
        };

        /* Other stair IDs
         * Listed in order: north, west, south, east
         * Use this for stairs not in the proper sequence
         */
        private static readonly int[] m_StairIDs =
        {
            0x71F, 0x736, 0x737, 0x749,
            0x35D4, 0x35D3, 0x35D6, 0x35D5,
            0x360B, 0x360A, 0x360D, 0x360C,
            0x4360, 0x435E, 0x435F, 0x4361,
            0x435C, 0x435A, 0x435B, 0x435D,
            0x4364, 0x4362, 0x4363, 0x4365,
            0x4B05, 0x4B04, 0x4B34, 0x4B33,
            0x7809, 0x7808, 0x780A, 0x780B,
            0x7BB, 0x7BC
        };

        private DesignState m_Backup;  // State at last user backup.
        private DesignState m_Current; // State which is currently visible.

        private int m_DefaultPrice;
        private DesignState m_Design; // State of current design.

        public HouseFoundation(Mobile owner, int multiID, int maxLockdowns, int maxSecures)
            : base(multiID, owner, maxLockdowns, maxSecures)
        {
            SignpostGraphic = 9;

            Fixtures = new List<Item>();

            var x = Components.Min.X;
            var y = Components.Height - 1 - Components.Center.Y;

            SignHanger = new Static(0xB98);
            SignHanger.MoveToWorld(new Point3D(X + x, Y + y, Z + 7), Map);

            CheckSignpost();

            SetSign(x, y, 7);
        }

        public HouseFoundation(Serial serial)
            : base(serial)
        {
        }

        public FoundationType Type { get; set; }

        public int LastRevision { get; set; }

        public List<Item> Fixtures { get; private set; }

        public Item SignHanger { get; private set; }

        public Item Signpost { get; private set; }

        public int SignpostGraphic { get; set; }

        public Mobile Customizer { get; set; }

        public override bool IsAosRules => true;

        public override bool IsActive => Customizer == null;

        public virtual int CustomizationCost => Core.AOS ? 0 : 10000;

        public override MultiComponentList Components
        {
            get
            {
                if (m_Current == null)
                {
                    SetInitialState();
                }

                return m_Current.Components;
            }
        }

        public DesignState CurrentState
        {
            get
            {
                if (m_Current == null)
                {
                    SetInitialState();
                }

                return m_Current;
            }
            set => m_Current = value;
        }

        public DesignState DesignState
        {
            get
            {
                if (m_Design == null)
                {
                    SetInitialState();
                }

                return m_Design;
            }
            set => m_Design = value;
        }

        public DesignState BackupState
        {
            get
            {
                if (m_Backup == null)
                {
                    SetInitialState();
                }

                return m_Backup;
            }
            set => m_Backup = value;
        }

        public override Rectangle2D[] Area
        {
            get
            {
                var mcl = Components;

                return new[] { new Rectangle2D(mcl.Min.X, mcl.Min.Y, mcl.Width, mcl.Height) };
            }
        }

        public override Point3D BaseBanLocation =>
            new(Components.Min.X, Components.Height - 1 - Components.Center.Y, 0);

        public override int DefaultPrice => m_DefaultPrice;

        public int MaxLevels
        {
            get
            {
                var mcl = Components;

                if (mcl.Width >= 14 || mcl.Height >= 14)
                {
                    return 4;
                }

                return 3;
            }
        }

        public static ComponentVerification Verification => m_Verification ?? (m_Verification = new ComponentVerification());

        public bool IsFixture(Item item) => Fixtures.Contains(item);

        public override int GetMaxUpdateRange() => 24;

        public override int GetUpdateRange(Mobile m)
        {
            var w = CurrentState.Components.Width;
            var h = CurrentState.Components.Height - 1;
            var v = 18 + (w > h ? w : h) / 2;

            if (v > 24)
            {
                v = 24;
            }
            else if (v < 18)
            {
                v = 18;
            }

            return v;
        }

        public void SetInitialState()
        {
            // This is a new house, it has not yet loaded a design state
            m_Current = new DesignState(this, GetEmptyFoundation());
            m_Design = new DesignState(m_Current);
            m_Backup = new DesignState(m_Current);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            SignHanger?.Delete();

            Signpost?.Delete();

            if (Fixtures == null)
            {
                return;
            }

            for (var i = 0; i < Fixtures.Count; ++i)
            {
                var item = Fixtures[i];

                item?.Delete();
            }

            Fixtures.Clear();
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            var x = Location.X - oldLocation.X;
            var y = Location.Y - oldLocation.Y;
            var z = Location.Z - oldLocation.Z;

            SignHanger?.MoveToWorld(new Point3D(SignHanger.X + x, SignHanger.Y + y, SignHanger.Z + z), Map);

            Signpost?.MoveToWorld(new Point3D(Signpost.X + x, Signpost.Y + y, Signpost.Z + z), Map);

            if (Fixtures == null)
            {
                return;
            }

            for (var i = 0; i < Fixtures.Count; ++i)
            {
                var item = Fixtures[i];

                if (item is BaseDoor door && Doors.Contains(door))
                {
                    continue;
                }

                item.MoveToWorld(new Point3D(item.X + x, item.Y + y, item.Z + z), Map);
            }
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (SignHanger != null)
            {
                SignHanger.Map = Map;
            }

            if (Signpost != null)
            {
                Signpost.Map = Map;
            }

            if (Fixtures == null)
            {
                return;
            }

            for (var i = 0; i < Fixtures.Count; ++i)
            {
                Fixtures[i].Map = Map;
            }
        }

        public void ClearFixtures(Mobile from)
        {
            if (Fixtures == null)
            {
                return;
            }

            RemoveKeys(from);

            for (var i = 0; i < Fixtures.Count; ++i)
            {
                var item = Fixtures[i];
                item.Delete();

                if (item is BaseDoor door)
                {
                    Doors.Remove(door);
                }
            }

            Fixtures.Clear();
        }

        public void AddFixtures(Mobile from, MultiTileEntry[] list)
        {
            Fixtures ??= new List<Item>();

            uint keyValue = 0;

            for (var i = 0; i < list.Length; ++i)
            {
                var mte = list[i];
                int itemID = mte.ItemId;

                if (itemID >= 0x181D && itemID < 0x1829)
                {
                    var tp = new HouseTeleporter(itemID);

                    AddFixture(tp, mte);
                }
                else
                {
                    BaseDoor door = null;

                    if (itemID >= 0x675 && itemID < 0x6F5)
                    {
                        var type = (itemID - 0x675) / 16;
                        var facing = (DoorFacing)((itemID - 0x675) / 2 % 8);

                        door = type switch
                        {
                            0 => new GenericHouseDoor(facing, 0x675, 0xEC, 0xF3),
                            1 => new GenericHouseDoor(facing, 0x685, 0xEC, 0xF3),
                            2 => new GenericHouseDoor(facing, 0x695, 0xEB, 0xF2),
                            3 => new GenericHouseDoor(facing, 0x6A5, 0xEA, 0xF1),
                            4 => new GenericHouseDoor(facing, 0x6B5, 0xEA, 0xF1),
                            5 => new GenericHouseDoor(facing, 0x6C5, 0xEC, 0xF3),
                            6 => new GenericHouseDoor(facing, 0x6D5, 0xEA, 0xF1),
                            _ => new GenericHouseDoor(facing, 0x6E5, 0xEA, 0xF1),
                        };
                    }
                    else if (itemID >= 0x314 && itemID < 0x364)
                    {
                        var type = (itemID - 0x314) / 16;
                        var facing = (DoorFacing)((itemID - 0x314) / 2 % 8);
                        door = new GenericHouseDoor(facing, 0x314 + type * 16, 0xED, 0xF4);
                    }
                    else if (itemID >= 0x824 && itemID < 0x834)
                    {
                        var facing = (DoorFacing)((itemID - 0x824) / 2 % 8);
                        door = new GenericHouseDoor(facing, 0x824, 0xEC, 0xF3);
                    }
                    else if (itemID >= 0x839 && itemID < 0x849)
                    {
                        var facing = (DoorFacing)((itemID - 0x839) / 2 % 8);
                        door = new GenericHouseDoor(facing, 0x839, 0xEB, 0xF2);
                    }
                    else if (itemID >= 0x84C && itemID < 0x85C)
                    {
                        var facing = (DoorFacing)((itemID - 0x84C) / 2 % 8);
                        door = new GenericHouseDoor(facing, 0x84C, 0xEC, 0xF3);
                    }
                    else if (itemID >= 0x866 && itemID < 0x876)
                    {
                        var facing = (DoorFacing)((itemID - 0x866) / 2 % 8);
                        door = new GenericHouseDoor(facing, 0x866, 0xEB, 0xF2);
                    }
                    else if (itemID >= 0xE8 && itemID < 0xF8)
                    {
                        var facing = (DoorFacing)((itemID - 0xE8) / 2 % 8);
                        door = new GenericHouseDoor(facing, 0xE8, 0xED, 0xF4);
                    }
                    else if (itemID >= 0x1FED && itemID < 0x1FFD)
                    {
                        var facing = (DoorFacing)((itemID - 0x1FED) / 2 % 8);
                        door = new GenericHouseDoor(facing, 0x1FED, 0xEC, 0xF3);
                    }
                    else if (itemID >= 0x241F && itemID < 0x2421)
                    {
                        // DoorFacing facing = (DoorFacing)(((itemID - 0x241F) / 2) % 8);
                        door = new GenericHouseDoor(DoorFacing.NorthCCW, 0x2415, -1, -1);
                    }
                    else if (itemID >= 0x2423 && itemID < 0x2425)
                    {
                        // DoorFacing facing = (DoorFacing)(((itemID - 0x241F) / 2) % 8);
                        // This one and the above one are 'special' cases, ie: OSI had the ItemID pattern discombobulated for these
                        door = new GenericHouseDoor(DoorFacing.WestCW, 0x2423, -1, -1);
                    }
                    else if (itemID >= 0x2A05 && itemID < 0x2A1D)
                    {
                        var facing = (DoorFacing)((itemID - 0x2A05) / 2 % 4 + 8);

                        var sound = itemID >= 0x2A0D && itemID < 0x2a15 ? 0x539 : -1;

                        door = new GenericHouseDoor(facing, 0x29F5 + 8 * ((itemID - 0x2A05) / 8), sound, sound);
                    }
                    else if (itemID == 0x2D46)
                    {
                        door = new GenericHouseDoor(DoorFacing.NorthCW, 0x2D46, 0xEA, 0xF1, false);
                    }
                    else if (itemID is 0x2D48 or 0x2FE2)
                    {
                        door = new GenericHouseDoor(DoorFacing.SouthCCW, itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x2D63 && itemID < 0x2D70)
                    {
                        var mod = (itemID - 0x2D63) / 2 % 2;
                        var facing = mod == 0 ? DoorFacing.SouthCCW : DoorFacing.WestCCW;

                        var type = (itemID - 0x2D63) / 4;

                        door = new GenericHouseDoor(facing, 0x2D63 + 4 * type + mod * 2, 0xEA, 0xF1, false);
                    }
                    else if (itemID is 0x2FE4 or 0x31AE)
                    {
                        door = new GenericHouseDoor(DoorFacing.WestCCW, itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x319C && itemID < 0x31AE)
                    {
                        // special case for 0x31aa <-> 0x31a8 (a9)

                        var mod = (itemID - 0x319C) / 2 % 2;

                        var specialCase = itemID is 0x31AA or 0x31A8;

                        DoorFacing facing;

                        if (itemID is 0x31AA or 0x31A8)
                        {
                            facing = mod == 0 ? DoorFacing.NorthCW : DoorFacing.EastCW;
                        }
                        else
                        {
                            facing = mod == 0 ? DoorFacing.EastCW : DoorFacing.NorthCW;
                        }

                        var type = (itemID - 0x319C) / 4;

                        door = new GenericHouseDoor(facing, 0x319C + 4 * type + mod * 2, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x367B && itemID < 0x369B)
                    {
                        var type = (itemID - 0x367B) / 16;
                        var facing = (DoorFacing)((itemID - 0x367B) / 2 % 8);

                        door = type switch
                        {
                            0 => new GenericHouseDoor(facing, 0x367B, 0xED, 0xF4),
                            _ => new GenericHouseDoor(facing, 0x368B, 0xEC, 0x3E7),
                        };
                    }
                    else if (itemID >= 0x409B && itemID < 0x40A3)
                    {
                        door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x409B), itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x410C && itemID < 0x4114)
                    {
                        door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x410C), itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x41C2 && itemID < 0x41CA)
                    {
                        door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x41C2), itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x41CF && itemID < 0x41D7)
                    {
                        door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x41CF), itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x436E && itemID < 0x437E)
                    {
                        /* These ones had to be different...
                         * Offset 0 2 4 6 8 10 12 14
                         * DoorFacing 2 3 2 3 6 7 6 7
                         */
                        var offset = itemID - 0x436E;
                        var facing = (DoorFacing)((offset / 2 + 2 * ((1 + offset / 4) % 2)) % 8);
                        door = new GenericHouseDoor(facing, itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x46DD && itemID < 0x46E5)
                    {
                        door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x46DD), itemID, 0xEB, 0xF2, false);
                    }
                    else if (itemID >= 0x4D22 && itemID < 0x4D2A)
                    {
                        door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x4D22), itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x50C8 && itemID < 0x50D0)
                    {
                        door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x50C8), itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x50D0 && itemID < 0x50D8)
                    {
                        door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x50D0), itemID, 0xEA, 0xF1, false);
                    }
                    else if (itemID >= 0x5142 && itemID < 0x514A)
                    {
                        door = new GenericHouseDoor(GetSADoorFacing(itemID - 0x5142), itemID, 0xF0, 0xEF, false);
                    }

                    if (door != null)
                    {
                        if (keyValue == 0)
                        {
                            keyValue = CreateKeys(from);
                        }

                        door.Locked = true;
                        door.KeyValue = keyValue;

                        AddDoor(door, mte.OffsetX, mte.OffsetY, mte.OffsetZ);
                        Fixtures.Add(door);
                    }
                }
            }

            for (var i = 0; i < Fixtures.Count; ++i)
            {
                var fixture = Fixtures[i];

                if (fixture is HouseTeleporter tp)
                {
                    for (var j = 1; j <= Fixtures.Count; ++j)
                    {
                        if (Fixtures[(i + j) % Fixtures.Count] is HouseTeleporter check && check.ItemID == tp.ItemID)
                        {
                            tp.Target = check;
                            break;
                        }
                    }
                }
                else if (fixture is BaseHouseDoor door)
                {
                    if (door.Link?.Deleted == false)
                    {
                        continue;
                    }

                    DoorFacing linkFacing;
                    int xOffset, yOffset;

                    switch (door.Facing)
                    {
                        default:
                            {
                                linkFacing = DoorFacing.EastCCW;
                                xOffset = 1;
                                yOffset = 0;
                                break;
                            }
                        case DoorFacing.EastCCW:
                            {
                                linkFacing = DoorFacing.WestCW;
                                xOffset = -1;
                                yOffset = 0;
                                break;
                            }
                        case DoorFacing.WestCCW:
                            {
                                linkFacing = DoorFacing.EastCW;
                                xOffset = 1;
                                yOffset = 0;
                                break;
                            }
                        case DoorFacing.EastCW:
                            {
                                linkFacing = DoorFacing.WestCCW;
                                xOffset = -1;
                                yOffset = 0;
                                break;
                            }
                        case DoorFacing.SouthCW:
                            {
                                linkFacing = DoorFacing.NorthCCW;
                                xOffset = 0;
                                yOffset = -1;
                                break;
                            }
                        case DoorFacing.NorthCCW:
                            {
                                linkFacing = DoorFacing.SouthCW;
                                xOffset = 0;
                                yOffset = 1;
                                break;
                            }
                        case DoorFacing.SouthCCW:
                            {
                                linkFacing = DoorFacing.NorthCW;
                                xOffset = 0;
                                yOffset = -1;
                                break;
                            }
                        case DoorFacing.NorthCW:
                            {
                                linkFacing = DoorFacing.SouthCCW;
                                xOffset = 0;
                                yOffset = 1;
                                break;
                            }
                        case DoorFacing.SouthSW:
                            {
                                linkFacing = DoorFacing.SouthSE;
                                xOffset = 1;
                                yOffset = 0;
                                break;
                            }
                        case DoorFacing.SouthSE:
                            {
                                linkFacing = DoorFacing.SouthSW;
                                xOffset = -1;
                                yOffset = 0;
                                break;
                            }
                        case DoorFacing.WestSN:
                            {
                                linkFacing = DoorFacing.WestSS;
                                xOffset = 0;
                                yOffset = 1;
                                break;
                            }
                        case DoorFacing.WestSS:
                            {
                                linkFacing = DoorFacing.WestSN;
                                xOffset = 0;
                                yOffset = -1;
                                break;
                            }
                    }

                    for (var j = i + 1; j < Fixtures.Count; ++j)
                    {
                        if (Fixtures[j] is BaseHouseDoor check && check.Link?.Deleted == false && check.Facing == linkFacing &&
                            check.X - door.X == xOffset && check.Y - door.Y == yOffset && check.Z == door.Z)
                        {
                            check.Link = door;
                            door.Link = check;
                            break;
                        }
                    }
                }
            }
        }

        private static DoorFacing GetSADoorFacing(int offset) => (DoorFacing)((offset / 2 + 2 * (1 + offset / 4)) % 8);

        public void AddFixture(Item item, MultiTileEntry mte)
        {
            Fixtures.Add(item);
            item.MoveToWorld(new Point3D(X + mte.OffsetX, Y + mte.OffsetY, Z + mte.OffsetZ), Map);
        }

        public static void GetFoundationGraphics(
            FoundationType type, out int east, out int south, out int post,
            out int corner
        )
        {
            switch (type)
            {
                default:
                    {
                        corner = 0x0014;
                        east = 0x0015;
                        south = 0x0016;
                        post = 0x0017;
                        break;
                    }
                case FoundationType.LightWood:
                    {
                        corner = 0x00BD;
                        east = 0x00BE;
                        south = 0x00BF;
                        post = 0x00C0;
                        break;
                    }
                case FoundationType.Dungeon:
                    {
                        corner = 0x02FD;
                        east = 0x02FF;
                        south = 0x02FE;
                        post = 0x0300;
                        break;
                    }
                case FoundationType.Brick:
                    {
                        corner = 0x0041;
                        east = 0x0043;
                        south = 0x0042;
                        post = 0x0044;
                        break;
                    }
                case FoundationType.Stone:
                    {
                        corner = 0x0065;
                        east = 0x0064;
                        south = 0x0063;
                        post = 0x0066;
                        break;
                    }

                case FoundationType.ElvenGrey:
                    {
                        corner = 0x2DF7;
                        east = 0x2DF9;
                        south = 0x2DFA;
                        post = 0x2DF8;
                        break;
                    }
                case FoundationType.ElvenNatural:
                    {
                        corner = 0x2DFB;
                        east = 0x2DFD;
                        south = 0x2DFE;
                        post = 0x2DFC;
                        break;
                    }

                case FoundationType.Crystal:
                    {
                        corner = 0x3672;
                        east = 0x3671;
                        south = 0x3670;
                        post = 0x3673;
                        break;
                    }
                case FoundationType.Shadow:
                    {
                        corner = 0x3676;
                        east = 0x3675;
                        south = 0x3674;
                        post = 0x3677;
                        break;
                    }
            }
        }

        public static void ApplyFoundation(FoundationType type, MultiComponentList mcl)
        {
            GetFoundationGraphics(type, out var east, out var south, out var post, out var corner);

            var xCenter = mcl.Center.X;
            var yCenter = mcl.Center.Y;

            mcl.Add(post, 0 - xCenter, 0 - yCenter, 0);
            mcl.Add(corner, mcl.Width - 1 - xCenter, mcl.Height - 2 - yCenter, 0);

            for (var x = 1; x < mcl.Width; ++x)
            {
                mcl.Add(south, x - xCenter, 0 - yCenter, 0);

                if (x < mcl.Width - 1)
                {
                    mcl.Add(south, x - xCenter, mcl.Height - 2 - yCenter, 0);
                }
            }

            for (var y = 1; y < mcl.Height - 1; ++y)
            {
                mcl.Add(east, 0 - xCenter, y - yCenter, 0);

                if (y < mcl.Height - 2)
                {
                    mcl.Add(east, mcl.Width - 1 - xCenter, y - yCenter, 0);
                }
            }
        }

        public static void AddStairsTo(ref MultiComponentList mcl)
        {
            // copy the original..
            mcl = new MultiComponentList(mcl);

            mcl.Resize(mcl.Width, mcl.Height + 1);

            var xCenter = mcl.Center.X;
            var yCenter = mcl.Center.Y;
            var y = mcl.Height - 1;

            for (var x = 0; x < mcl.Width; ++x)
            {
                mcl.Add(0x63, x - xCenter, y - yCenter, 0);
            }
        }

        public MultiComponentList GetEmptyFoundation()
        {
            // Copy original foundation layout
            var mcl = new MultiComponentList(MultiData.GetComponents(ItemID));

            mcl.Resize(mcl.Width, mcl.Height + 1);

            var xCenter = mcl.Center.X;
            var yCenter = mcl.Center.Y;
            var y = mcl.Height - 1;

            ApplyFoundation(Type, mcl);

            for (var x = 1; x < mcl.Width; ++x)
            {
                mcl.Add(0x751, x - xCenter, y - yCenter, 0);
            }

            return mcl;
        }

        public void CheckSignpost()
        {
            var mcl = Components;

            var x = mcl.Min.X;
            var y = mcl.Height - 2 - mcl.Center.Y;

            if (CheckWall(mcl, x, y))
            {
                Signpost?.Delete();

                Signpost = null;
            }
            else if (Signpost == null)
            {
                Signpost = new Static(SignpostGraphic);
                Signpost.MoveToWorld(new Point3D(X + x, Y + y, Z + 7), Map);
            }
            else
            {
                Signpost.ItemID = SignpostGraphic;
                Signpost.MoveToWorld(new Point3D(X + x, Y + y, Z + 7), Map);
            }
        }

        public bool CheckWall(MultiComponentList mcl, int x, int y)
        {
            x += mcl.Center.X;
            y += mcl.Center.Y;

            if (x >= 0 && x < mcl.Width && y >= 0 && y < mcl.Height)
            {
                var tiles = mcl.Tiles[x][y];

                for (var i = 0; i < tiles.Length; ++i)
                {
                    var tile = tiles[i];

                    if (tile.Z == 7 && tile.Height == 20)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void BeginCustomize(Mobile m)
        {
            if (!m.CheckAlive())
            {
                return;
            }

            if (SpellHelper.CheckCombat(m))
            {
                m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return;
            }

            RelocateEntities();

            foreach (var item in GetItems())
            {
                item.Location = BanLocation;
            }

            foreach (var mobile in GetMobiles())
            {
                if (mobile != m)
                {
                    mobile.Location = BanLocation;
                }
            }

            DesignContext.Add(m, this);
            m.NetState.SendBeginHouseCustomization(Serial);

            var ns = m.NetState;
            if (ns != null)
            {
                SendInfoTo(ns);
            }

            DesignState.SendDetailedInfoTo(ns);
        }

        public override void SendInfoTo(NetState ns, ReadOnlySpan<byte> world = default)
        {
            base.SendInfoTo(ns, world);

            var stateToSend = DesignContext.Find(ns?.Mobile)?.Foundation == this ? DesignState : CurrentState;
            stateToSend.SendGeneralInfoTo(ns);
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.Write(5); // version

            writer.Write(Signpost);
            writer.Write(SignpostGraphic);

            writer.Write((int)Type);

            writer.Write(SignHanger);

            writer.Write(LastRevision);
            Fixtures.Tidy();
            writer.Write(Fixtures);

            CurrentState.Serialize(writer);
            DesignState.Serialize(writer);
            BackupState.Serialize(writer);

            base.Serialize(writer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 5:
                case 4:
                    {
                        Signpost = reader.ReadEntity<Item>();
                        SignpostGraphic = reader.ReadInt();

                        goto case 3;
                    }
                case 3:
                    {
                        Type = (FoundationType)reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        SignHanger = reader.ReadEntity<Item>();

                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 5)
                        {
                            m_DefaultPrice = reader.ReadInt();
                        }

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 3)
                        {
                            Type = FoundationType.Stone;
                        }

                        if (version < 4)
                        {
                            SignpostGraphic = 9;
                        }

                        LastRevision = reader.ReadInt();
                        Fixtures = reader.ReadEntityList<Item>();

                        m_Current = new DesignState(this, reader);
                        m_Design = new DesignState(this, reader);
                        m_Backup = new DesignState(this, reader);

                        break;
                    }
            }

            base.Deserialize(reader);
        }

        public bool IsHiddenToCustomizer(Item item) =>
            item == Signpost || item == SignHanger || item == Sign || IsFixture(item);

        public static unsafe void Initialize()
        {
            IncomingExtendedCommandPackets.RegisterExtended(0x1E, true, &QueryDesignDetails);

            IncomingPackets.RegisterEncoded(0x02, true, &Designer_Backup);
            IncomingPackets.RegisterEncoded(0x03, true, &Designer_Restore);
            IncomingPackets.RegisterEncoded(0x04, true, &Designer_Commit);
            IncomingPackets.RegisterEncoded(0x05, true, &Designer_Delete);
            IncomingPackets.RegisterEncoded(0x06, true, &Designer_Build);
            IncomingPackets.RegisterEncoded(0x0C, true, &Designer_Close);
            IncomingPackets.RegisterEncoded(0x0D, true, &Designer_Stairs);
            IncomingPackets.RegisterEncoded(0x0E, true, &Designer_Sync);
            IncomingPackets.RegisterEncoded(0x10, true, &Designer_Clear);
            IncomingPackets.RegisterEncoded(0x12, true, &Designer_Level);

            IncomingPackets.RegisterEncoded(0x13, true, &Designer_Roof);       // Samurai Empire roof
            IncomingPackets.RegisterEncoded(0x14, true, &Designer_RoofDelete); // Samurai Empire roof

            IncomingPackets.RegisterEncoded(0x1A, true, &Designer_Revert);

            EventSink.Speech += EventSink_Speech;
        }

        private static void EventSink_Speech(SpeechEventArgs e)
        {
            if (DesignContext.Find(e.Mobile) != null)
            {
                e.Mobile.SendLocalizedMessage(1061925); // You cannot speak while customizing your house.
                e.Blocked = true;
            }
        }

        public static void Designer_Sync(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;

            /* Client requested state synchronization
               *  - Resend full house state
               */

            // Resend full house state
            DesignContext.Find(from)?.Foundation.DesignState.SendDetailedInfoTo(state);
        }

        public static void Designer_Clear(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null)
            {
                return;
            }

            /* Client chose to clear the design
               *  - Restore empty foundation
               *     - Construct new design state from empty foundation
               *     - Assign constructed state to foundation
               *  - Update revision
               *  - Update client with new state
               */

            // Restore empty foundation : Construct new design state from empty foundation
            var newDesign = new DesignState(context.Foundation, context.Foundation.GetEmptyFoundation());

            // Restore empty foundation : Assign constructed state to foundation
            context.Foundation.DesignState = newDesign;

            // Update revision
            newDesign.OnRevised();

            // Update client with new state
            context.Foundation.SendInfoTo(state);
            newDesign.SendDetailedInfoTo(state);
        }

        public static void Designer_Restore(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null)
            {
                return;
            }

            /* Client chose to restore design to the last backup state
               *  - Restore backup
               *     - Construct new design state from backup state
               *     - Assign constructed state to foundation
               *  - Update revision
               *  - Update client with new state
               */

            // Restore backup : Construct new design state from backup state
            var backupDesign = new DesignState(context.Foundation.BackupState);

            // Restore backup : Assign constructed state to foundation
            context.Foundation.DesignState = backupDesign;

            // Update revision;
            backupDesign.OnRevised();

            // Update client with new state
            context.Foundation.SendInfoTo(state);
            backupDesign.SendDetailedInfoTo(state);
        }

        public static void Designer_Backup(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null)
            {
                return;
            }

            /* Client chose to backup design state
               *  - Construct a copy of the current design state
               *  - Assign constructed state to backup state field
               */

            // Construct a copy of the current design state
            var copyState = new DesignState(context.Foundation.DesignState);

            // Assign constructed state to backup state field
            context.Foundation.BackupState = copyState;
        }

        public static void Designer_Revert(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null)
            {
                return;
            }

            /* Client chose to revert design state to currently visible state
               *  - Revert design state
               *     - Construct a copy of the current visible state
               *     - Freeze fixtures in constructed state
               *     - Assign constructed state to foundation
               *     - If a signpost is needed, add it
               *  - Update revision
               *  - Update client with new state
               */

            // Revert design state : Construct a copy of the current visible state
            var copyState = new DesignState(context.Foundation.CurrentState);

            // Revert design state : Freeze fixtures in constructed state
            copyState.FreezeFixtures();

            // Revert design state : Assign constructed state to foundation
            context.Foundation.DesignState = copyState;

            // Revert design state : If a signpost is needed, add it
            context.Foundation.CheckSignpost();

            // Update revision
            copyState.OnRevised();

            // Update client with new state
            context.Foundation.SendInfoTo(state);
            copyState.SendDetailedInfoTo(state);
        }

        public void EndConfirmCommit(Mobile from)
        {
            var oldPrice = Price;
            var newPrice = oldPrice + CustomizationCost +
                           (DesignState.Components.List.Length -
                            (CurrentState.Components.List.Length + CurrentState.Fixtures.Length)) * 500;
            var cost = newPrice - oldPrice;

            if (!Deleted)
            {
                // Temporary Fix. We should be booting a client out of customization mode in the delete handler.
                if (from.AccessLevel >= AccessLevel.GameMaster && cost != 0)
                {
                    if (cost > 0)
                    {
                        from.SendMessage($"{cost} gold would have been withdrawn from your bank if you were not a GM.");
                    }
                    else
                    {
                        from.SendMessage($"{cost} gold would have been deposited into your bank if you were not a GM.");
                    }
                }
                else
                {
                    if (cost > 0)
                    {
                        if (Banker.Withdraw(from, cost))
                        {
                            from.SendLocalizedMessage(
                                1060398,
                                cost.ToString()
                            ); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                        }
                        else
                        {
                            from.SendLocalizedMessage(
                                1061903
                            ); // You cannot commit this house design, because you do not have the necessary funds in your bank box to pay for the upgrade.  Please back up your design, obtain the required funds, and commit your design again.
                            return;
                        }
                    }
                    else if (cost < 0)
                    {
                        if (Banker.Deposit(from, -cost))
                        {
                            from.SendLocalizedMessage(
                                1060397,
                                (-cost).ToString()
                            ); // ~1_AMOUNT~ gold has been deposited into your bank box.
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            /* Client chose to commit current design state
               *  - Commit design state
               *     - Construct a copy of the current design state
               *     - Clear visible fixtures
               *     - Melt fixtures from constructed state
               *     - Add melted fixtures from constructed state
               *     - Assign constructed state to foundation
               *  - Update house price
               *  - Remove design context
               *  - Notify the client that customization has ended
               *  - Notify the core that the foundation has changed and should be resent to all clients
               *  - If a signpost is needed, add it
               *  - Eject all from house
               *  - Restore relocated entities
               */

            // Commit design state : Construct a copy of the current design state
            var copyState = new DesignState(DesignState);

            // Commit design state : Clear visible fixtures
            ClearFixtures(from);

            // Commit design state : Melt fixtures from constructed state
            copyState.MeltFixtures();

            // Commit design state : Add melted fixtures from constructed state
            AddFixtures(from, copyState.Fixtures);

            // Commit design state : Assign constructed state to foundation
            CurrentState = copyState;

            // Update house price
            Price = newPrice - CustomizationCost;

            // Remove design context
            DesignContext.Remove(from);

            // Notify the client that customization has ended
            from.NetState.SendEndHouseCustomization(Serial);

            // Notify the core that the foundation has changed and should be resent to all clients
            Delta(ItemDelta.Update);
            ProcessDelta();
            CurrentState.SendDetailedInfoTo(from.NetState);

            // If a signpost is needed, add it
            CheckSignpost();

            // Eject all from house
            from.RevealingAction();

            foreach (var item in GetItems())
            {
                item.Location = BanLocation;
            }

            foreach (var mobile in GetMobiles())
            {
                mobile.Location = BanLocation;
            }

            // Restore relocated entities
            RestoreRelocatedEntities();
        }

        public static void Designer_Commit(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context != null)
            {
                var oldPrice = context.Foundation.Price;
                var newPrice = oldPrice + context.Foundation.CustomizationCost +
                               (context.Foundation.DesignState.Components.List.Length -
                                (context.Foundation.CurrentState.Components.List.Length +
                                 context.Foundation.Fixtures.Count)) * 500;
                var bankBalance = Banker.GetBalance(from);

                from.SendGump(new ConfirmCommitGump(context.Foundation, bankBalance, oldPrice, newPrice));
            }
        }

        public static int GetLevelZ(int level, HouseFoundation house)
        {
            if (level < 1 || level > house.MaxLevels)
            {
                level = 1;
            }

            return (level - 1) * 20 + 7;
        }

        public static int GetZLevel(int z, HouseFoundation house)
        {
            var level = (z - 7) / 20 + 1;

            if (level < 1 || level > house.MaxLevels)
            {
                level = 1;
            }

            return level;
        }

        public static bool ValidPiece(int itemID, bool roof = false)
        {
            itemID &= TileData.MaxItemValue;
            return roof == TileData.ItemTable[itemID].Roof && Verification.IsItemValid(itemID);
        }

        public static bool IsStairBlock(int id)
        {
            var delta = -1;

            for (var i = 0; delta < 0 && i < m_BlockIDs.Length; ++i)
            {
                delta = m_BlockIDs[i] - id;
            }

            return delta == 0;
        }

        public static bool IsStair(int id, ref int dir)
        {
            // dir n=0 w=1 s=2 e=3
            var delta = -4;

            for (var i = 0; delta < -3 && i < m_StairSeqs.Length; ++i)
            {
                delta = m_StairSeqs[i] - id;
            }

            if (delta >= -3 && delta <= 0)
            {
                dir = -delta;
                return true;
            }

            for (var i = 0; i < m_StairIDs.Length; ++i)
            {
                if (m_StairIDs[i] == id)
                {
                    dir = i % 4;
                    return true;
                }
            }

            return false;
        }

        public static bool DeleteStairs(MultiComponentList mcl, int id, int x, int y, int z)
        {
            var ax = x + mcl.Center.X;
            var ay = y + mcl.Center.Y;

            if (ax < 0 || ay < 0 || ax >= mcl.Width || ay >= mcl.Height - 1 || z < 7 || (z - 7) % 5 != 0)
            {
                return false;
            }

            if (IsStairBlock(id))
            {
                var tiles = mcl.Tiles[ax][ay];

                for (var i = 0; i < tiles.Length; ++i)
                {
                    var tile = tiles[i];

                    if (tile.Z == z + 5)
                    {
                        id = tile.ID;
                        z = tile.Z;

                        if (!IsStairBlock(id))
                        {
                            break;
                        }
                    }
                }
            }

            var dir = 0;

            if (!IsStair(id, ref dir))
            {
                return false;
            }

            if (AllowStairSectioning)
            {
                return true; // skip deletion
            }

            var height = (z - 7) % 20 / 5;

            int xStart, yStart;
            int xInc, yInc;

            switch (dir)
            {
                default:
                    {
                        xStart = x;
                        yStart = y + height;
                        xInc = 0;
                        yInc = -1;
                        break;
                    }
                case 1: // West
                    {
                        xStart = x + height;
                        yStart = y;
                        xInc = -1;
                        yInc = 0;
                        break;
                    }
                case 2: // South
                    {
                        xStart = x;
                        yStart = y - height;
                        xInc = 0;
                        yInc = 1;
                        break;
                    }
                case 3: // East
                    {
                        xStart = x - height;
                        yStart = y;
                        xInc = 1;
                        yInc = 0;
                        break;
                    }
            }

            var zStart = z - height * 5;

            for (var i = 0; i < 4; ++i)
            {
                x = xStart + i * xInc;
                y = yStart + i * yInc;

                for (var j = 0; j <= i; ++j)
                {
                    mcl.RemoveXYZH(x, y, zStart + j * 5, 5);
                }

                ax = x + mcl.Center.X;
                ay = y + mcl.Center.Y;

                if (ax >= 1 && ax < mcl.Width && ay >= 1 && ay < mcl.Height - 1)
                {
                    var tiles = mcl.Tiles[ax][ay];

                    var hasBaseFloor = false;

                    for (var j = 0; !hasBaseFloor && j < tiles.Length; ++j)
                    {
                        hasBaseFloor = tiles[j].Z == 7 && tiles[j].ID != 1;
                    }

                    if (!hasBaseFloor)
                    {
                        mcl.Add(0x31F4, x, y, 7);
                    }
                }
            }

            return true;
        }

        public static void Designer_Delete(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null)
            {
                return;
            }

            /* Client chose to delete a component
             *  - Read data detailing which component to delete
             *  - Verify component is deletable
             *  - Remove the component
             *  - If needed, replace removed component with a dirt tile
             *  - Update revision
             */

            // Read data detailing which component to delete
            var itemID = reader.ReadInt32();
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            var z = reader.ReadInt32();

            // Verify component is deletable
            var design = context.Foundation.DesignState;
            var mcl = design.Components;

            var ax = x + mcl.Center.X;
            var ay = y + mcl.Center.Y;

            if (z == 0 && ax >= 0 && ax < mcl.Width && ay >= 0 && ay < mcl.Height - 1)
            {
                /* Component is not deletable
                 *  - Resend design state
                 *  - Return without further processing
                 */

                design.SendDetailedInfoTo(state);
                return;
            }

            var fixState = false;

            // Remove the component
            if (AllowStairSectioning)
            {
                if (DeleteStairs(mcl, itemID, x, y, z))
                {
                    fixState = true; // The client removes the entire set of stairs locally, resend state
                }

                mcl.Remove(itemID, x, y, z);
            }
            else
            {
                if (!DeleteStairs(mcl, itemID, x, y, z))
                {
                    mcl.Remove(itemID, x, y, z);
                }
            }

            // If needed, replace removed component with a dirt tile
            if (ax >= 1 && ax < mcl.Width && ay >= 1 && ay < mcl.Height - 1)
            {
                var tiles = mcl.Tiles[ax][ay];

                var hasBaseFloor = false;

                for (var i = 0; !hasBaseFloor && i < tiles.Length; ++i)
                {
                    hasBaseFloor = tiles[i].Z == 7 && tiles[i].ID != 1;
                }

                if (!hasBaseFloor)
                {
                    mcl.Add(0x31F4, x, y, 7);
                }
            }

            // Update revision
            design.OnRevised();

            // Resend design state
            if (fixState)
            {
                design.SendDetailedInfoTo(state);
            }
        }

        public static void Designer_Stairs(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null)
            {
                return;
            }

            /* Client chose to add stairs
               *  - Read data detailing stair type and location
               *  - Validate stair multi ID
               *  - Add the stairs
               *     - Load data describing the stair components
               *     - Insert described components
               *  - Update revision
               */

            // Read data detailing stair type and location
            var itemID = reader.ReadInt32();
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();

            // Validate stair multi ID
            var design = context.Foundation.DesignState;

            if (!Verification.IsMultiValid(itemID))
            {
                /* Specified multi ID is not a stair
                   *  - Resend design state
                   *  - Return without further processing
                   */

                TraceValidity(state, itemID);
                design.SendDetailedInfoTo(state);
                return;
            }

            // Add the stairs
            var mcl = design.Components;

            // Add the stairs : Load data describing stair components
            var stairs = MultiData.GetComponents(itemID);

            // Add the stairs : Insert described components
            var z = GetLevelZ(context.Level, context.Foundation);

            for (var i = 0; i < stairs.List.Length; ++i)
            {
                var entry = stairs.List[i];

                if (entry.ItemId != 1)
                {
                    mcl.Add(entry.ItemId, x + entry.OffsetX, y + entry.OffsetY, z + entry.OffsetZ);
                }
            }

            // Update revision
            design.OnRevised();
        }

        private static void TraceValidity(NetState state, int itemID)
        {
            try
            {
                using var op = new StreamWriter("comp_val.log", true);
                op.WriteLine("{0}\t{1}\tInvalid ItemID 0x{2:X4}", state, state.Mobile, itemID);
            }
            catch
            {
                // ignored
            }
        }

        public static void Designer_Build(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null)
            {
                return;
            }

            /* Client chose to add a component
             *  - Read data detailing component graphic and location
             *  - Add component
             *  - Update revision
             */

            // Read data detailing component graphic and location
            var itemID = reader.ReadInt32();
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();

            // Add component
            var design = context.Foundation.DesignState;

            if (from.AccessLevel < AccessLevel.GameMaster && !ValidPiece(itemID))
            {
                TraceValidity(state, itemID);
                design.SendDetailedInfoTo(state);
                return;
            }

            var mcl = design.Components;

            var z = GetLevelZ(context.Level, context.Foundation);

            if (y + mcl.Center.Y == mcl.Height - 1)
            {
                z = 0; // Tiles placed on the far-south of the house are at 0 Z
            }

            mcl.Add(itemID, x, y, z);

            // Update revision
            design.OnRevised();
        }

        public static void Designer_Close(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null)
            {
                return;
            }

            /* Client closed his house design window
             *  - Remove design context
             *  - Notify the client that customization has ended
             *  - Refresh client with current visible design state
             *  - If a signpost is needed, add it
             *  - Eject all from house
             *  - Restore relocated entities
             */

            // Remove design context
            DesignContext.Remove(from);

            // Notify the client that customization has ended
            from.NetState.SendEndHouseCustomization(context.Foundation.Serial);

            // Refresh client with current visible design state
            context.Foundation.SendInfoTo(state);
            context.Foundation.CurrentState.SendDetailedInfoTo(state);

            // If a signpost is needed, add it
            context.Foundation.CheckSignpost();

            // Eject all from house
            from.RevealingAction();

            foreach (var item in context.Foundation.GetItems())
            {
                item.Location = context.Foundation.BanLocation;
            }

            foreach (var mobile in context.Foundation.GetMobiles())
            {
                mobile.Location = context.Foundation.BanLocation;
            }

            // Restore relocated entities
            context.Foundation.RestoreRelocatedEntities();
        }

        public static void Designer_Level(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null)
            {
                return;
            }

            /* Client is moving to a new floor level
             *  - Read data detailing the target level
             *  - Validate target level
             *  - Update design context with new level
             *  - Teleport mobile to new level
             *  - Update client
             *
             */

            // Read data detailing the target level
            var newLevel = reader.ReadInt32();

            // Validate target level
            if (newLevel < 1 || newLevel > context.MaxLevels)
            {
                newLevel = 1;
            }

            // Update design context with new level
            context.Level = newLevel;

            // Teleport mobile to new level
            from.Location = new Point3D(from.X, from.Y, context.Foundation.Z + GetLevelZ(newLevel, context.Foundation));

            // Update client
            context.Foundation.SendInfoTo(state);
        }

        public static void QueryDesignDetails(NetState state, SpanReader reader, int packetLength)
        {
            var from = state.Mobile;

            if (World.FindItem((Serial)reader.ReadUInt32()) is HouseFoundation foundation && from.Map == foundation.Map &&
                from.InRange(foundation.GetWorldLocation(), 24) &&
                from.CanSee(foundation))
            {
                var stateToSend = DesignContext.Find(from)?.Foundation == foundation
                    ? foundation.DesignState
                    : foundation.CurrentState;
                stateToSend.SendDetailedInfoTo(state);
            }
        }

        public static void Designer_Roof(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            if (context == null || !Core.SE && from.AccessLevel < AccessLevel.GameMaster)
            {
                return;
            }

            // Read data detailing component graphic and location
            var itemID = reader.ReadInt32();
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            var z = reader.ReadInt32();

            // Add component
            var design = context.Foundation.DesignState;

            if (from.AccessLevel < AccessLevel.GameMaster && !ValidPiece(itemID, true))
            {
                TraceValidity(state, itemID);
                design.SendDetailedInfoTo(state);
                return;
            }

            var mcl = design.Components;

            if (z is < -3 or > 12 || z % 3 != 0)
            {
                z = -3;
            }

            z += GetLevelZ(context.Level, context.Foundation);

            var list = mcl.List;
            for (var i = 0; i < list.Length; i++)
            {
                var mte = list[i];

                if (mte.OffsetX == x && mte.OffsetY == y &&
                    GetZLevel(mte.OffsetZ, context.Foundation) == context.Level &&
                    TileData.ItemTable[mte.ItemId & TileData.MaxItemValue].Roof)
                {
                    mcl.Remove(mte.ItemId, x, y, mte.OffsetZ);
                }
            }

            mcl.Add(itemID, x, y, z);

            // Update revision
            design.OnRevised();
        }

        public static void Designer_RoofDelete(NetState state, IEntity e, EncodedReader reader)
        {
            var from = state.Mobile;
            var context = DesignContext.Find(from);

            // No need to check for Core.SE if trying to remove something that shouldn't be able to be placed anyways
            if (context == null)
            {
                return;
            }

            // Read data detailing which component to delete
            var itemID = reader.ReadInt32();
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            var z = reader.ReadInt32();

            // Verify component is deletable
            var design = context.Foundation.DesignState;
            var mcl = design.Components;

            if (!TileData.ItemTable[itemID & TileData.MaxItemValue].Roof)
            {
                design.SendDetailedInfoTo(state);
                return;
            }

            mcl.Remove(itemID, x, y, z);

            design.OnRevised();
        }
    }

    public class DesignState
    {
        public DesignState(HouseFoundation foundation, MultiComponentList components)
        {
            Foundation = foundation;
            Components = components;
            Fixtures = Array.Empty<MultiTileEntry>();
        }

        public DesignState(DesignState toCopy)
        {
            Foundation = toCopy.Foundation;
            Components = new MultiComponentList(toCopy.Components);
            Revision = toCopy.Revision;
            Fixtures = new MultiTileEntry[toCopy.Fixtures.Length];

            for (var i = 0; i < Fixtures.Length; ++i)
            {
                Fixtures[i] = toCopy.Fixtures[i];
            }
        }

        public DesignState(HouseFoundation foundation, IGenericReader reader)
        {
            Foundation = foundation;

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Components = new MultiComponentList(reader);

                        var length = reader.ReadInt();

                        Fixtures = new MultiTileEntry[length];

                        for (var i = 0; i < length; ++i)
                        {
                            Fixtures[i].ItemId = reader.ReadUShort();
                            Fixtures[i].OffsetX = reader.ReadShort();
                            Fixtures[i].OffsetY = reader.ReadShort();
                            Fixtures[i].OffsetZ = reader.ReadShort();
                            Fixtures[i].Flags = (TileFlag)reader.ReadInt();
                        }

                        Revision = reader.ReadInt();

                        break;
                    }
            }
        }

        public byte[] PacketCache { get; set; }

        public HouseFoundation Foundation { get; }

        public MultiComponentList Components { get; }

        public MultiTileEntry[] Fixtures { get; private set; }

        public int Revision { get; set; }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version

            Components.Serialize(writer);

            writer.Write(Fixtures.Length);

            for (var i = 0; i < Fixtures.Length; ++i)
            {
                var ent = Fixtures[i];

                writer.Write(ent.ItemId);
                writer.Write(ent.OffsetX);
                writer.Write(ent.OffsetY);
                writer.Write(ent.OffsetZ);
                writer.Write((int)ent.Flags);
            }

            writer.Write(Revision);
        }

        public void OnRevised()
        {
            Revision = ++Foundation.LastRevision;
            PacketCache = null;
        }

        public void SendGeneralInfoTo(NetState state) => state.SendDesignStateGeneral(Foundation.Serial, Revision);
        public void SendDetailedInfoTo(NetState state) => state?.Send(
            PacketCache ??= HousePackets.CreateHouseDesignStateDetailed(
                Foundation.Serial,
                Revision,
                Components
            )
        );

        public void FreezeFixtures()
        {
            OnRevised();

            for (var i = 0; i < Fixtures.Length; ++i)
            {
                var mte = Fixtures[i];

                Components.Add(mte.ItemId, mte.OffsetX, mte.OffsetY, mte.OffsetZ);
            }

            Fixtures = Array.Empty<MultiTileEntry>();
        }

        public void MeltFixtures()
        {
            OnRevised();

            var list = Components.List;
            var length = 0;

            for (var i = list.Length - 1; i >= 0; --i)
            {
                var mte = list[i];

                if (IsFixture(mte.ItemId))
                {
                    ++length;
                }
            }

            Fixtures = new MultiTileEntry[length];

            for (var i = list.Length - 1; i >= 0; --i)
            {
                var mte = list[i];

                if (IsFixture(mte.ItemId))
                {
                    Fixtures[--length] = mte;
                    Components.Remove(mte.ItemId, mte.OffsetX, mte.OffsetY, mte.OffsetZ);
                }
            }
        }

        public static bool IsFixture(int itemID)
        {
            if (itemID >= 0x675 && itemID < 0x6F5)
            {
                return true;
            }

            if (itemID >= 0x314 && itemID < 0x364)
            {
                return true;
            }

            if (itemID >= 0x824 && itemID < 0x834)
            {
                return true;
            }

            if (itemID >= 0x839 && itemID < 0x849)
            {
                return true;
            }

            if (itemID >= 0x84C && itemID < 0x85C)
            {
                return true;
            }

            if (itemID >= 0x866 && itemID < 0x876)
            {
                return true;
            }

            if (itemID >= 0x0E8 && itemID < 0x0F8)
            {
                return true;
            }

            if (itemID >= 0x1FED && itemID < 0x1FFD)
            {
                return true;
            }

            if (itemID >= 0x181D && itemID < 0x1829)
            {
                return true;
            }

            if (itemID >= 0x241F && itemID < 0x2421)
            {
                return true;
            }

            if (itemID >= 0x2423 && itemID < 0x2425)
            {
                return true;
            }

            if (itemID >= 0x2A05 && itemID < 0x2A1D)
            {
                return true;
            }

            if (itemID >= 0x319C && itemID < 0x31B0)
            {
                return true;
            }

            // ML doors
            if (itemID is 0x2D46 or 0x2D48 or 0x2FE2 or 0x2FE4)
            {
                return true;
            }

            if (itemID >= 0x2D63 && itemID < 0x2D70)
            {
                return true;
            }

            if (itemID >= 0x319C && itemID < 0x31AF)
            {
                return true;
            }

            if (itemID >= 0x367B && itemID < 0x369B)
            {
                return true;
            }

            // SA doors
            if (itemID >= 0x409B && itemID < 0x40A3)
            {
                return true;
            }

            if (itemID >= 0x410C && itemID < 0x4114)
            {
                return true;
            }

            if (itemID >= 0x41C2 && itemID < 0x41CA)
            {
                return true;
            }

            if (itemID >= 0x41CF && itemID < 0x41D7)
            {
                return true;
            }

            if (itemID >= 0x436E && itemID < 0x437E)
            {
                return true;
            }

            if (itemID >= 0x46DD && itemID < 0x46E5)
            {
                return true;
            }

            if (itemID >= 0x4D22 && itemID < 0x4D2A)
            {
                return true;
            }

            if (itemID >= 0x50C8 && itemID < 0x50D8)
            {
                return true;
            }

            if (itemID >= 0x5142 && itemID < 0x514A)
            {
                return true;
            }

            // TOL doors
            if (itemID >= 0x9AD7 && itemID < 0x9AE7)
            {
                return true;
            }

            return itemID >= 0x9B3C && itemID < 0x9B4C;
        }
    }

    public class ConfirmCommitGump : Gump
    {
        private readonly HouseFoundation m_Foundation;

        public ConfirmCommitGump(HouseFoundation foundation, int bankBalance, int oldPrice, int newPrice) : base(50, 50)
        {
            m_Foundation = foundation;

            AddPage(0);

            AddBackground(0, 0, 320, 320, 5054);

            AddImageTiled(10, 10, 300, 20, 2624);
            AddImageTiled(10, 40, 300, 240, 2624);
            AddImageTiled(10, 290, 300, 20, 2624);

            AddAlphaRegion(10, 10, 300, 300);

            AddHtmlLocalized(10, 10, 300, 20, 1062060, 32736); // <CENTER>COMMIT DESIGN</CENTER>

            AddHtmlLocalized(10, 40, 300, 140, newPrice - oldPrice <= bankBalance ? 1061898 : 1061903, 1023, false, true);

            AddHtmlLocalized(10, 190, 150, 20, 1061902, 32736); // Bank Balance:
            AddLabel(170, 190, 55, bankBalance.ToString());

            AddHtmlLocalized(10, 215, 150, 20, 1061899, 1023); // Old Value:
            AddLabel(170, 215, 90, oldPrice.ToString());

            AddHtmlLocalized(10, 235, 150, 20, 1061900, 1023); // Cost To Commit:
            AddLabel(170, 235, 90, newPrice.ToString());

            if (newPrice - oldPrice < 0)
            {
                AddHtmlLocalized(10, 260, 150, 20, 1062059, 992); // Your Refund:
                AddLabel(170, 260, 70, (oldPrice - newPrice).ToString());
            }
            else
            {
                AddHtmlLocalized(10, 260, 150, 20, 1061901, 31744); // Your Cost:
                AddLabel(170, 260, 40, (newPrice - oldPrice).ToString());
            }

            AddButton(10, 290, 4005, 4007, 1);
            AddHtmlLocalized(45, 290, 55, 20, 1011036, 32767); // OKAY

            AddButton(170, 290, 4005, 4007, 0);
            AddHtmlLocalized(195, 290, 55, 20, 1011012, 32767); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                m_Foundation.EndConfirmCommit(sender.Mobile);
            }
        }
    }

    public class DesignContext
    {
        public DesignContext(HouseFoundation foundation)
        {
            Foundation = foundation;
            Level = 1;
        }

        public HouseFoundation Foundation { get; }

        public int Level { get; set; }

        public int MaxLevels => Foundation.MaxLevels;

        public static Dictionary<Mobile, DesignContext> Table { get; } = new();

        public static DesignContext Find(Mobile from)
        {
            if (from == null)
            {
                return null;
            }

            Table.TryGetValue(from, out var d);

            return d;
        }

        public static bool Check(Mobile m)
        {
            if (Find(m) == null)
            {
                return true;
            }

            m.SendLocalizedMessage(1062206); // You cannot do that while customizing a house.
            return false;
        }

        public static void Add(Mobile from, HouseFoundation foundation)
        {
            if (from == null)
            {
                return;
            }

            var c = new DesignContext(foundation);

            Table[from] = c;

            if (from is PlayerMobile pm)
            {
                pm.DesignContext = c;
            }

            foundation.Customizer = from;

            from.Hidden = true;
            from.Location = new Point3D(foundation.X, foundation.Y, foundation.Z + 7);

            var state = from.NetState;

            if (state == null)
            {
                return;
            }

            var fixtures = foundation.Fixtures;

            for (var i = 0; i < fixtures?.Count; ++i)
            {
                var item = fixtures[i];

                state.SendRemoveEntity(item.Serial);
            }

            if (foundation.Signpost != null)
            {
                state.SendRemoveEntity(foundation.Signpost.Serial);
            }

            if (foundation.SignHanger != null)
            {
                state.SendRemoveEntity(foundation.SignHanger.Serial);
            }

            if (foundation.Sign != null)
            {
                state.SendRemoveEntity(foundation.Sign.Serial);
            }
        }

        public static void Remove(Mobile from)
        {
            var context = Find(from);

            if (context == null)
            {
                return;
            }

            Table.Remove(from);

            if (from is PlayerMobile pm)
            {
                pm.DesignContext = null;
            }

            context.Foundation.Customizer = null;

            var state = from.NetState;

            if (state == null)
            {
                return;
            }

            var fixtures = context.Foundation.Fixtures;

            for (var i = 0; i < fixtures?.Count; ++i)
            {
                var item = fixtures[i];

                item.SendInfoTo(state);
            }

            context.Foundation.Signpost?.SendInfoTo(state);
            context.Foundation.SignHanger?.SendInfoTo(state);
            context.Foundation.Sign?.SendInfoTo(state);
        }
    }
}
