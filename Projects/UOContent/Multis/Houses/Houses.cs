using Server.Items;
using Server.Multis.Deeds;

namespace Server.Multis
{
    public class SmallOldHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray = { new(-3, -3, 7, 7), new(-1, 4, 3, 1) };

        public SmallOldHouse(Mobile owner, int id) : base(id, owner, 425, 3)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoor(0, 3, 7, keyValue);

            SetSign(2, 4, 5);
        }

        public SmallOldHouse(Serial serial) : base(serial)
        {
        }

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(2, 4, 0);

        public override int DefaultPrice => 43800;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.TwoStoryFoundations[0];

        public override HouseDeed GetDeed()
        {
            return ItemID switch
            {
                0x64 => new StonePlasterHouseDeed(),
                0x66 => new FieldStoneHouseDeed(),
                0x68 => new SmallBrickHouseDeed(),
                0x6A => new WoodHouseDeed(),
                0x6C => new WoodPlasterHouseDeed(),
                0x6E => new ThatchedRoofCottageDeed(),
                _    => new ThatchedRoofCottageDeed()
            };
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

    public class GuildHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray = { new(-7, -7, 14, 14), new(-2, 7, 4, 1) };

        public GuildHouse(Mobile owner) : base(0x74, owner, 1100, 8)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoors(-1, 6, 7, keyValue);

            SetSign(4, 8, 16);

            AddSouthDoor(-3, -1, 7);
            AddSouthDoor(3, -1, 7);
        }

        public GuildHouse(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 144500;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.ThreeStoryFoundations[20];
        public override int ConvertOffsetX => -1;
        public override int ConvertOffsetY => -1;

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(4, 8, 0);

        public override HouseDeed GetDeed() => new BrickHouseDeed();

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

    public class TwoStoryHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray =
            { new(-7, 0, 14, 7), new(-7, -7, 9, 7), new(-4, 7, 4, 1) };

        public TwoStoryHouse(Mobile owner, int id) : base(id, owner, 1370, 10)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoors(-3, 6, 7, keyValue);

            SetSign(2, 8, 16);

            AddSouthDoor(-3, 0, 7);
            AddSouthDoor(id == 0x76 ? -2 : -3, 0, 27);
        }

        public TwoStoryHouse(Serial serial) : base(serial)
        {
        }

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(2, 8, 0);

        public override int DefaultPrice => 192400;

        public override HouseDeed GetDeed()
        {
            return ItemID switch
            {
                0x76 => new TwoStoryWoodPlasterHouseDeed(),
                0x78 => new TwoStoryStonePlasterHouseDeed(),
                _    => new TwoStoryStonePlasterHouseDeed()
            };
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

    public class Tower : BaseHouse
    {
        public static Rectangle2D[] AreaArray =
        {
            new(-7, -7, 16, 14), new(-1, 7, 4, 2), new(-11, 0, 4, 7),
            new(9, 0, 4, 7)
        };

        public Tower(Mobile owner) : base(0x7A, owner, 2119, 15)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoors(false, 0, 6, 6, keyValue);

            SetSign(5, 8, 16);

            AddSouthDoor(false, 3, -2, 6);
            AddEastDoor(false, 1, 4, 26);
            AddEastDoor(false, 1, 4, 46);
        }

        public Tower(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 433200;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.ThreeStoryFoundations[37];
        public override int ConvertOffsetY => -1;

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(5, 8, 0);

        public override HouseDeed GetDeed() => new TowerDeed();

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

    public class Keep : BaseHouse // warning: ODD shape!
    {
        public static Rectangle2D[] AreaArray =
        {
            new(-11, -11, 7, 8), new(-11, 5, 7, 8), new(6, -11, 7, 8),
            new(6, 5, 7, 8), new(-9, -3, 5, 8), new(6, -3, 5, 8),
            new(-4, -9, 10, 20), new(-1, 11, 4, 1)
        };

        public Keep(Mobile owner) : base(0x7C, owner, 2625, 18)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoors(false, 0, 10, 6, keyValue);

            SetSign(5, 12, 16);
        }

        public Keep(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 665200;

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(5, 13, 0);

        public override HouseDeed GetDeed() => new KeepDeed();

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

    public class Castle : BaseHouse
    {
        public static Rectangle2D[] AreaArray = { new(-15, -15, 31, 31), new(-1, 16, 4, 1) };

        public Castle(Mobile owner) : base(0x7E, owner, 4076, 28)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoors(false, 0, 15, 6, keyValue);

            SetSign(5, 17, 16);

            AddSouthDoors(false, 0, 11, 6, true);
            AddSouthDoors(false, 0, 5, 6, false);
            AddSouthDoors(false, -1, -11, 6, false);
        }

        public Castle(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 1022800;

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(5, 17, 0);

        public override HouseDeed GetDeed() => new CastleDeed();

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

    public class LargePatioHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray = { new(-7, -7, 15, 14), new(-5, 7, 4, 1) };

        public LargePatioHouse(Mobile owner) : base(0x8C, owner, 1100, 8)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoors(-4, 6, 7, keyValue);

            SetSign(1, 8, 16);

            AddEastDoor(1, 4, 7);
            AddEastDoor(1, -4, 7);
            AddSouthDoor(4, -1, 7);
        }

        public LargePatioHouse(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 152800;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.ThreeStoryFoundations[29];
        public override int ConvertOffsetY => -1;

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(1, 8, 0);

        public override HouseDeed GetDeed() => new LargePatioDeed();

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

    public class LargeMarbleHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray = { new(-7, -7, 15, 14), new(-6, 7, 6, 1) };

        public LargeMarbleHouse(Mobile owner) : base(0x96, owner, 1370, 10)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoors(false, -4, 3, 4, keyValue);

            SetSign(1, 8, 11);
        }

        public LargeMarbleHouse(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 192000;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.ThreeStoryFoundations[29];
        public override int ConvertOffsetY => -1;

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(1, 8, 0);

        public override HouseDeed GetDeed() => new LargeMarbleDeed();

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

    public class SmallTower : BaseHouse
    {
        public static Rectangle2D[] AreaArray = { new(-3, -3, 8, 7), new(2, 4, 3, 1) };

        public SmallTower(Mobile owner) : base(0x98, owner, 580, 4)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoor(false, 3, 3, 6, keyValue);

            SetSign(1, 4, 5);
        }

        public SmallTower(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 88500;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.TwoStoryFoundations[6];

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(1, 4, 0);

        public override HouseDeed GetDeed() => new SmallTowerDeed();

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

    public class LogCabin : BaseHouse
    {
        public static Rectangle2D[] AreaArray = { new(-3, -6, 8, 13) };

        public LogCabin(Mobile owner) : base(0x9A, owner, 1100, 8)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoor(1, 4, 8, keyValue);

            SetSign(5, 8, 20);

            AddSouthDoor(1, 0, 29);
        }

        public LogCabin(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 97800;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.TwoStoryFoundations[12];

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(5, 8, 0);

        public override HouseDeed GetDeed() => new LogCabinDeed();

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

    public class SandStonePatio : BaseHouse
    {
        public static Rectangle2D[] AreaArray = { new(-5, -4, 12, 8), new(-2, 4, 3, 1) };

        public SandStonePatio(Mobile owner) : base(0x9C, owner, 850, 6)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoor(-1, 3, 6, keyValue);

            SetSign(4, 6, 24);
        }

        public SandStonePatio(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 90900;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.TwoStoryFoundations[35];
        public override int ConvertOffsetY => -1;

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(4, 6, 0);

        public override HouseDeed GetDeed() => new SandstonePatioDeed();

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

    public class TwoStoryVilla : BaseHouse
    {
        public static Rectangle2D[] AreaArray = { new(-5, -5, 11, 11), new(2, 6, 4, 1) };

        public TwoStoryVilla(Mobile owner) : base(0x9E, owner, 1100, 8)
        {
            var keyValue = CreateKeys(owner);

            AddSouthDoors(3, 1, 5, keyValue);

            SetSign(3, 8, 24);

            AddEastDoor(1, 0, 25);
            AddSouthDoor(-3, -1, 25);
        }

        public TwoStoryVilla(Serial serial) : base(serial)
        {
        }

        public override int DefaultPrice => 136500;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.TwoStoryFoundations[31];

        public override Rectangle2D[] Area => AreaArray;
        public override Point3D BaseBanLocation => new(3, 8, 0);

        public override HouseDeed GetDeed() => new VillaDeed();

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

    public class SmallShop : BaseHouse
    {
        public static Rectangle2D[] AreaArray1 = { new(-3, -3, 7, 7), new(-1, 4, 4, 1) };
        public static Rectangle2D[] AreaArray2 = { new(-3, -3, 7, 7), new(-2, 4, 3, 1) };

        public SmallShop(Mobile owner, int id) : base(id, owner, 425, 3)
        {
            var keyValue = CreateKeys(owner);

            var door = MakeDoor(false, DoorFacing.EastCW);

            door.Locked = true;
            door.KeyValue = keyValue;

            if (door is BaseHouseDoor houseDoor)
            {
                houseDoor.Facing = DoorFacing.EastCCW;
            }

            AddDoor(door, -2, 0, id == 0xA2 ? 24 : 27);

            // AddSouthDoor( false, -2, 0, 27 - (id == 0xA2 ? 3 : 0), keyValue );

            SetSign(3, 4, 7 - (id == 0xA2 ? 2 : 0));
        }

        public SmallShop(Serial serial) : base(serial)
        {
        }

        public override Rectangle2D[] Area => ItemID == 0x40A2 ? AreaArray1 : AreaArray2;
        public override Point3D BaseBanLocation => new(3, 4, 0);

        public override int DefaultPrice => 63000;

        public override HousePlacementEntry ConvertEntry => HousePlacementEntry.TwoStoryFoundations[0];

        public override HouseDeed GetDeed()
        {
            return ItemID switch
            {
                0xA0 => new StoneWorkshopDeed(),
                0xA2 => new MarbleWorkshopDeed(),
                _    => new MarbleWorkshopDeed()
            };
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
}
