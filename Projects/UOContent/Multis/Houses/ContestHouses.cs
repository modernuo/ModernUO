using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Logging;

namespace Server.Multis
{
    public enum ContestHouseType
    {
        Keep,
        Castle,
        Other
    }

    public class BaseContestHouse : BaseHouse
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(BaseContestHouse));

        public BaseContestHouse(ContestHouseType type, int multiID, Mobile owner, int maxLockDown, int maxSecure)
            : base(multiID, owner, maxLockDown, maxSecure)
        {
            HouseType = type;

            AutoAddFixtures();
        }

        public BaseContestHouse(Serial serial) : base(serial)
        {
        }

        public ContestHouseType HouseType { get; set; }
        public List<Item> Fixtures { get; private set; }

        public virtual int SignPostID => 9;

        public override Point3D BaseBanLocation => new(
            Components.Min.X,
            Components.Height - 1 - Components.Center.Y,
            0
        );

        public override Rectangle2D[] Area
        {
            get
            {
                var mcl = Components;
                return new[] { new Rectangle2D(mcl.Min.X, mcl.Min.Y, mcl.Width, mcl.Height) };
            }
        }

        protected void SetSign(int xOffset, int yOffset, int zOffset, bool post)
        {
            SetSign(xOffset, yOffset, zOffset);

            var hanger = new Static(0xB9E);
            hanger.MoveToWorld(new Point3D(X + xOffset, Y + yOffset, Z + zOffset), Map);

            AddFixture(hanger);

            if (post)
            {
                var signPost = new Static(SignPostID);
                signPost.MoveToWorld(new Point3D(X + xOffset, Y + yOffset - 1, Z + zOffset), Map);

                AddFixture(signPost);
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (Fixtures == null)
            {
                return;
            }

            foreach (var item in Fixtures)
            {
                item.Delete();
            }
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            var x = Location.X - oldLocation.X;
            var y = Location.Y - oldLocation.Y;
            var z = Location.Z - oldLocation.Z;

            if (Fixtures == null)
            {
                return;
            }

            foreach (var item in Fixtures)
            {
                item.Location = new Point3D(item.X + x, item.Y + y, item.Z + z);
            }
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (Fixtures == null)
            {
                return;
            }

            foreach (var item in Fixtures)
            {
                item.Map = Map;
            }
        }

        public void AddTeleporters(int id, Point3D offset1, Point3D offset2)
        {
            var tele1 = new HouseTeleporter(id);
            var tele2 = new HouseTeleporter(id);

            tele1.Target = tele2;
            tele2.Target = tele1;

            tele1.MoveToWorld(new Point3D(X + offset1.X, Y + offset1.Y, offset1.Z), Map);
            tele2.MoveToWorld(new Point3D(X + offset2.X, Y + offset2.Y, offset2.Z), Map);

            AddFixture(tele1);
            AddFixture(tele2);
        }

        public void AddFixture(Item item)
        {
            Fixtures ??= new List<Item>();

            Fixtures.Add(item);
        }

        public override bool IsInside(Point3D p, int height)
        {
            if (base.IsInside(p, height))
            {
                return true;
            }

            if (Fixtures == null)
            {
                return false;
            }

            foreach (Item fixture in Fixtures)
            {
                if (fixture is HouseTeleporter fix && fix.Location == p)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void AutoAddFixtures()
        {
            var components = MultiData.GetComponents(ItemID);

            var teleporters = new Dictionary<int, List<MultiTileEntry>>();

            foreach (var entry in components.List.Where(e => e.Flags == 0))
                // Teleporters
            {
                if (entry.ItemId >= 0x181D && entry.ItemId <= 0x1828)
                {
                    if (teleporters.TryGetValue(entry.ItemId, out var result))
                    {
                        result.Add(entry);
                    }
                    else
                    {
                        teleporters.Add(entry.ItemId, new List<MultiTileEntry> { entry });
                    }
                }
                else
                {
                    var data = TileData.ItemTable[entry.ItemId & TileData.MaxItemValue];

                    // door
                    if (data.Door)
                    {
                        AddDoor(entry.ItemId, entry.OffsetX, entry.OffsetY, entry.OffsetZ);
                    }
                    else
                    {
                        Item st = new Static(entry.ItemId);

                        st.MoveToWorld(new Point3D(X + entry.OffsetX, Y + entry.OffsetY, entry.OffsetZ), Map);
                        AddFixture(st);
                    }
                }
            }

            foreach (var door in Doors)
            {
                foreach (var check in Doors.Where(d => d != door))
                {
                    if (door.InRange(check.Location, 1))
                    {
                        door.Link = check;
                        check.Link = door;
                    }
                }
            }

            foreach (var (key, value) in teleporters)
            {
                if (value.Count > 2)
                {
                    logger.Warning("More than 2 teleporters detected for {ItemId:X}!", key);
                }
                else if (value.Count <= 1)
                {
                    logger.Warning("1 or less teleporters detected for {ItemId:X}!", key);

                    continue;
                }

                AddTeleporters(
                    key,
                    new Point3D(value[0].OffsetX, value[0].OffsetY, value[0].OffsetZ),
                    new Point3D(value[1].OffsetX, value[1].OffsetY, value[1].OffsetZ)
                );
            }

            teleporters.Clear();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write((int)HouseType);

            if (Fixtures != null)
            {
                Fixtures.Tidy();
                writer.Write(Fixtures);
            }
            else
            {
                writer.Write(0);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            HouseType = (ContestHouseType)reader.ReadInt();

            var count = reader.ReadInt();

            for (var i = 0; i < count; i++)
            {
                AddFixture(reader.ReadEntity<Item>());
            }
        }
    }

    public class TrinsicKeep : BaseContestHouse
    {
        public static Rectangle2D[] AreaArray =
        {
            new(-11, -11, 23, 23), new(-10, 13, 6, 1),
            new(-2, 13, 6, 1), new(6, 13, 7, 1)
        };

        public TrinsicKeep(Mobile owner)
            : base(ContestHouseType.Keep, 0x147E, owner, 2113, 18)
        {
            SetSign(-11, 13, 7, false);
        }

        public TrinsicKeep(Serial serial) : base(serial)
        {
        }

        public override Rectangle2D[] Area => AreaArray;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class GothicRoseCastle : BaseContestHouse
    {
        public static Rectangle2D[] AreaArray =
        {
            new(-15, -15, 31, 31),
            new(-14, 16, 11, 1),
            new(-2, 16, 6, 1),
            new(5, 16, 11, 1)
        };

        public GothicRoseCastle(Mobile owner)
            : base(ContestHouseType.Castle, 0x147F, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, false);
        }

        public GothicRoseCastle(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class ElsaCastle : BaseContestHouse
    {
        public ElsaCastle(Mobile owner)
            : base(ContestHouseType.Castle, 0x1480, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, false);
        }

        public ElsaCastle(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class Spires : BaseContestHouse
    {
        public Spires(Mobile owner)
            : base(ContestHouseType.Castle, 0x1481, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, false);
        }

        public Spires(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class CastleOfOceania : BaseContestHouse
    {
        public CastleOfOceania(Mobile owner)
            : base(ContestHouseType.Castle, 0x1482, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, false);
        }

        public CastleOfOceania(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class FeudalCastle : BaseContestHouse
    {
        public static Rectangle2D[] AreaArray =
        {
            new(-15, -15, 31, 31),
            new(5, 16, 1, 1),
            new(7, 16, 4, 1),
            new(12, 16, 1, 1)
        };

        public FeudalCastle(Mobile owner)
            : base(ContestHouseType.Castle, 0x1483, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, true);
        }

        public FeudalCastle(Serial serial) : base(serial)
        {
        }

        public override Rectangle2D[] Area => AreaArray;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class RobinsNest : BaseContestHouse
    {
        public RobinsNest(Mobile owner)
            : base(ContestHouseType.Keep, 0x1484, owner, 2113, 18)
        {
            SetSign(-11, 13, 7, false);
        }

        public RobinsNest(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class TraditionalKeep : BaseContestHouse
    {
        public static Rectangle2D[] AreaArray =
        {
            new(-11, -11, 23, 23),
            new(-10, 13, 6, 1),
            new(-2, 13, 6, 1),
            new(6, 13, 7, 1)
        };

        public TraditionalKeep(Mobile owner)
            : base(ContestHouseType.Keep, 0x1485, owner, 2113, 18)
        {
            SetSign(-11, 13, 7, false);
        }

        public TraditionalKeep(Serial serial) : base(serial)
        {
        }

        public override Rectangle2D[] Area => AreaArray;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class VillaCrowley : BaseContestHouse
    {
        public VillaCrowley(Mobile owner)
            : base(ContestHouseType.Keep, 0x1486, owner, 2113, 18)
        {
            SetSign(-11, 13, 7, true);
        }

        public VillaCrowley(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class DarkthornKeep : BaseContestHouse
    {
        public DarkthornKeep(Mobile owner)
            : base(ContestHouseType.Keep, 0x1487, owner, 2113, 18)
        {
            SetSign(-11, 13, 7, false);
        }

        public DarkthornKeep(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class SandalwoodKeep : BaseContestHouse
    {
        public SandalwoodKeep(Mobile owner)
            : base(ContestHouseType.Keep, 0x1488, owner, 2113, 18)
        {
            SetSign(-11, 13, 7, true);
        }

        public SandalwoodKeep(Serial serial) : base(serial)
        {
        }

        public override int SignPostID => 353;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class CasaMoga : BaseContestHouse
    {
        public CasaMoga(Mobile owner)
            : base(ContestHouseType.Keep, 0x1489, owner, 2113, 18)
        {
            SetSign(-11, 13, 7, false);
        }

        public CasaMoga(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class RobinsRoost : BaseContestHouse
    {
        public RobinsRoost(Mobile owner)
            : base(ContestHouseType.Castle, 0x148A, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, true);
        }

        public RobinsRoost(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class Camelot : BaseContestHouse
    {
        public Camelot(Mobile owner)
            : base(ContestHouseType.Castle, 0x148B, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, false);
        }

        public Camelot(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class LacrimaeInCaelo : BaseContestHouse
    {
        public LacrimaeInCaelo(Mobile owner)
            : base(ContestHouseType.Castle, 0x148C, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, false);
        }

        public LacrimaeInCaelo(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class OkinawaSweetDreamCastle : BaseContestHouse
    {
        public static Rectangle2D[] AreaArray =
        {
            new(-15, -15, 31, 31),
            new(-14, 16, 6, 1),
            new(-7, 16, 8, 1),
            new(10, 16, 5, 1)
        };

        public OkinawaSweetDreamCastle(Mobile owner)
            : base(ContestHouseType.Castle, 0x148D, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, true);
        }

        public OkinawaSweetDreamCastle(Serial serial) : base(serial)
        {
        }

        public override Rectangle2D[] Area => AreaArray;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class TheSandstoneCastle : BaseContestHouse
    {
        public TheSandstoneCastle(Mobile owner)
            : base(ContestHouseType.Castle, 0x148E, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, true);
        }

        public TheSandstoneCastle(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class GrimswindSisters : BaseContestHouse
    {
        public static Rectangle2D[] AreaArray =
        {
            new(-15, -15, 31, 31),
            new(-14, 16, 9, 1),
            new(-3, 16, 8, 1),
            new(7, 16, 9, 1)
        };

        public GrimswindSisters(Mobile owner)
            : base(ContestHouseType.Castle, 0x148F, owner, 3281, 28)
        {
            SetSign(-15, 16, 7, false);
        }

        public GrimswindSisters(Serial serial) : base(serial)
        {
        }

        public override Rectangle2D[] Area => AreaArray;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
