using ModernUO.Serialization;

namespace Server.Multis.Deeds;

[SerializationGenerator(0, false)]
public partial class StonePlasterHouseDeed : HouseDeed
{
    [Constructible]
    public StonePlasterHouseDeed() : base(0x64, new Point3D(0, 4, 0))
    {
    }

    public override int LabelNumber => 1041211;
    public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x64);
}

[SerializationGenerator(0, false)]
public partial class FieldStoneHouseDeed : HouseDeed
{
    [Constructible]
    public FieldStoneHouseDeed() : base(0x66, new Point3D(0, 4, 0))
    {
    }

    public override int LabelNumber => 1041212;
    public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x66);
}

[SerializationGenerator(0, false)]
public partial class SmallBrickHouseDeed : HouseDeed
{
    [Constructible]
    public SmallBrickHouseDeed() : base(0x68, new Point3D(0, 4, 0))
    {
    }

    public override int LabelNumber => 1041213;
    public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x68);
}

[SerializationGenerator(0, false)]
public partial class WoodHouseDeed : HouseDeed
{
    [Constructible]
    public WoodHouseDeed() : base(0x6A, new Point3D(0, 4, 0))
    {
    }

    public override int LabelNumber => 1041214;
    public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x6A);
}

[SerializationGenerator(0, false)]
public partial class WoodPlasterHouseDeed : HouseDeed
{
    [Constructible]
    public WoodPlasterHouseDeed() : base(0x6C, new Point3D(0, 4, 0))
    {
    }

    public override int LabelNumber => 1041215;
    public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x6C);
}

[SerializationGenerator(0, false)]
public partial class ThatchedRoofCottageDeed : HouseDeed
{
    [Constructible]
    public ThatchedRoofCottageDeed() : base(0x6E, new Point3D(0, 4, 0))
    {
    }

    public override int LabelNumber => 1041216;
    public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x6E);
}

[SerializationGenerator(0, false)]
public partial class BrickHouseDeed : HouseDeed
{
    [Constructible]
    public BrickHouseDeed() : base(0x74, new Point3D(-1, 7, 0))
    {
    }

    public override int LabelNumber => 1041219;
    public override Rectangle2D[] Area => GuildHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new GuildHouse(owner);
}

[SerializationGenerator(0, false)]
public partial class TwoStoryWoodPlasterHouseDeed : HouseDeed
{
    [Constructible]
    public TwoStoryWoodPlasterHouseDeed() : base(0x76, new Point3D(-3, 7, 0))
    {
    }

    public override int LabelNumber => 1041220;
    public override Rectangle2D[] Area => TwoStoryHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new TwoStoryHouse(owner, 0x76);
}

[SerializationGenerator(0, false)]
public partial class TwoStoryStonePlasterHouseDeed : HouseDeed
{
    [Constructible]
    public TwoStoryStonePlasterHouseDeed() : base(0x78, new Point3D(-3, 7, 0))
    {
    }

    public override int LabelNumber => 1041221;
    public override Rectangle2D[] Area => TwoStoryHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new TwoStoryHouse(owner, 0x78);
}

[SerializationGenerator(0, false)]
public partial class TowerDeed : HouseDeed
{
    [Constructible]
    public TowerDeed() : base(0x7A, new Point3D(0, 7, 0))
    {
    }

    public override int LabelNumber => 1041222;
    public override Rectangle2D[] Area => Tower.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new Tower(owner);
}

[SerializationGenerator(0, false)]
public partial class KeepDeed : HouseDeed
{
    [Constructible]
    public KeepDeed() : base(0x7C, new Point3D(0, 11, 0))
    {
    }

    public override int LabelNumber => 1041223;
    public override Rectangle2D[] Area => Keep.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new Keep(owner);
}

[SerializationGenerator(0, false)]
public partial class CastleDeed : HouseDeed
{
    [Constructible]
    public CastleDeed() : base(0x7E, new Point3D(0, 16, 0))
    {
    }

    public override int LabelNumber => 1041224;
    public override Rectangle2D[] Area => Castle.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new Castle(owner);
}

[SerializationGenerator(0, false)]
public partial class LargePatioDeed : HouseDeed
{
    [Constructible]
    public LargePatioDeed() : base(0x8C, new Point3D(-4, 7, 0))
    {
    }

    public override int LabelNumber => 1041231;
    public override Rectangle2D[] Area => LargePatioHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new LargePatioHouse(owner);
}

[SerializationGenerator(0, false)]
public partial class LargeMarbleDeed : HouseDeed
{
    [Constructible]
    public LargeMarbleDeed() : base(0x96, new Point3D(-4, 7, 0))
    {
    }

    public override int LabelNumber => 1041236;
    public override Rectangle2D[] Area => LargeMarbleHouse.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new LargeMarbleHouse(owner);
}

[SerializationGenerator(0, false)]
public partial class SmallTowerDeed : HouseDeed
{
    [Constructible]
    public SmallTowerDeed() : base(0x98, new Point3D(3, 4, 0))
    {
    }

    public override int LabelNumber => 1041237;
    public override Rectangle2D[] Area => SmallTower.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new SmallTower(owner);
}

[SerializationGenerator(0, false)]
public partial class LogCabinDeed : HouseDeed
{
    [Constructible]
    public LogCabinDeed() : base(0x9A, new Point3D(1, 6, 0))
    {
    }

    public override int LabelNumber => 1041238;
    public override Rectangle2D[] Area => LogCabin.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new LogCabin(owner);
}

[SerializationGenerator(0, false)]
public partial class SandstonePatioDeed : HouseDeed
{
    [Constructible]
    public SandstonePatioDeed() : base(0x9C, new Point3D(-1, 4, 0))
    {
    }

    public override int LabelNumber => 1041239;
    public override Rectangle2D[] Area => SandStonePatio.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new SandStonePatio(owner);
}

[SerializationGenerator(0, false)]
public partial class VillaDeed : HouseDeed
{
    [Constructible]
    public VillaDeed() : base(0x9E, new Point3D(3, 6, 0))
    {
    }

    public override int LabelNumber => 1041240;
    public override Rectangle2D[] Area => TwoStoryVilla.AreaArray;

    public override BaseHouse GetHouse(Mobile owner) => new TwoStoryVilla(owner);
}

[SerializationGenerator(0, false)]
public partial class StoneWorkshopDeed : HouseDeed
{
    [Constructible]
    public StoneWorkshopDeed() : base(0xA0, new Point3D(-1, 4, 0))
    {
    }

    public override int LabelNumber => 1041241;
    public override Rectangle2D[] Area => SmallShop.AreaArray2;

    public override BaseHouse GetHouse(Mobile owner) => new SmallShop(owner, 0xA0);
}

[SerializationGenerator(0, false)]
public partial class MarbleWorkshopDeed : HouseDeed
{
    [Constructible]
    public MarbleWorkshopDeed() : base(0xA2, new Point3D(-1, 4, 0))
    {
    }

    public override int LabelNumber => 1041242;
    public override Rectangle2D[] Area => SmallShop.AreaArray1;

    public override BaseHouse GetHouse(Mobile owner) => new SmallShop(owner, 0xA2);
}
