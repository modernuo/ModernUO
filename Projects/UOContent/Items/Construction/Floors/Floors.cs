using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseFloor : Item
{
    public BaseFloor(int itemID, int count) : base(Utility.Random(itemID, count)) => Movable = false;
}

[SerializationGenerator(0, false)]
public partial class StonePaversLight : BaseFloor
{
    [Constructible]
    public StonePaversLight() : base(0x519, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class StonePaversMedium : BaseFloor
{
    [Constructible]
    public StonePaversMedium() : base(0x51D, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class StonePaversDark : BaseFloor
{
    [Constructible]
    public StonePaversDark() : base(0x521, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class GreyFlagstones : BaseFloor
{
    [Constructible]
    public GreyFlagstones() : base(0x4FC, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class SandFlagstones : BaseFloor
{
    [Constructible]
    public SandFlagstones() : base(0x500, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MarbleFloor : BaseFloor
{
    [Constructible]
    public MarbleFloor() : base(0x50D, 2)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class GreenMarbleFloor : BaseFloor
{
    [Constructible]
    public GreenMarbleFloor() : base(0x50F, 2)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class GreyMarbleFloor : BaseFloor
{
    [Constructible]
    public GreyMarbleFloor() : base(0x511, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CobblestonesFloor : BaseFloor
{
    [Constructible]
    public CobblestonesFloor() : base(0x515, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class SandstoneFloorN : BaseFloor
{
    [Constructible]
    public SandstoneFloorN() : base(0x525, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class SandstoneFloorW : BaseFloor
{
    [Constructible]
    public SandstoneFloorW() : base(0x529, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DarkSandstoneFloorN : BaseFloor
{
    [Constructible]
    public DarkSandstoneFloorN() : base(0x52F, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DarkSandstoneFloorW : BaseFloor
{
    [Constructible]
    public DarkSandstoneFloorW() : base(0x533, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BricksFloor1 : BaseFloor
{
    [Constructible]
    public BricksFloor1() : base(0x4E2, 8)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BricksFloor2 : BaseFloor
{
    [Constructible]
    public BricksFloor2() : base(0x537, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CaveFloorCenter : BaseFloor
{
    [Constructible]
    public CaveFloorCenter() : base(0x53B, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CaveFloorSouth : BaseFloor
{
    [Constructible]
    public CaveFloorSouth() : base(0x541, 3)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CaveFloorEast : BaseFloor
{
    [Constructible]
    public CaveFloorEast() : base(0x544, 3)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CaveFloorWest : BaseFloor
{
    [Constructible]
    public CaveFloorWest() : base(0x54A, 3)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CaveFloorNorth : BaseFloor
{
    [Constructible]
    public CaveFloorNorth() : base(0x54D, 3)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MarblePavers : BaseFloor
{
    [Constructible]
    public MarblePavers() : base(0x495, 4)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BlueSlateFloorCenter : BaseFloor
{
    [Constructible]
    public BlueSlateFloorCenter() : base(0x49B, 1)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class GreySlateFloor : BaseFloor
{
    [Constructible]
    public GreySlateFloor() : base(0x49C, 1)
    {
    }
}
