using ModernUO.Serialization;

namespace Server.Items;

public enum DoorFacing
{
    WestCW,
    EastCCW,
    WestCCW,
    EastCW,
    SouthCW,
    NorthCCW,
    SouthCCW,
    NorthCW,

    // Sliding Doors
    SouthSW,
    SouthSE,
    WestSS,
    WestSN
}

[SerializationGenerator(0, false)]
public partial class IronGateShort : BaseDoor
{
    [Constructible]
    public IronGateShort(DoorFacing facing) : base(
        0x84c + 2 * (int)facing,
        0x84d + 2 * (int)facing,
        0xEC,
        0xF3,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class IronGate : BaseDoor
{
    [Constructible]
    public IronGate(DoorFacing facing) : base(
        0x824 + 2 * (int)facing,
        0x825 + 2 * (int)facing,
        0xEC,
        0xF3,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LightWoodGate : BaseDoor
{
    [Constructible]
    public LightWoodGate(DoorFacing facing) : base(
        0x839 + 2 * (int)facing,
        0x83A + 2 * (int)facing,
        0xEB,
        0xF2,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DarkWoodGate : BaseDoor
{
    [Constructible]
    public DarkWoodGate(DoorFacing facing) : base(
        0x866 + 2 * (int)facing,
        0x867 + 2 * (int)facing,
        0xEB,
        0xF2,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MetalDoor : BaseDoor
{
    [Constructible]
    public MetalDoor(DoorFacing facing) : base(
        0x675 + 2 * (int)facing,
        0x676 + 2 * (int)facing,
        0xEC,
        0xF3,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BarredMetalDoor : BaseDoor
{
    [Constructible]
    public BarredMetalDoor(DoorFacing facing) : base(
        0x685 + 2 * (int)facing,
        0x686 + 2 * (int)facing,
        0xEC,
        0xF3,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class BarredMetalDoor2 : BaseDoor
{
    [Constructible]
    public BarredMetalDoor2(DoorFacing facing) : base(
        0x1FED + 2 * (int)facing,
        0x1FEE + 2 * (int)facing,
        0xEC,
        0xF3,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class RattanDoor : BaseDoor
{
    [Constructible]
    public RattanDoor(DoorFacing facing) : base(
        0x695 + 2 * (int)facing,
        0x696 + 2 * (int)facing,
        0xEB,
        0xF2,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DarkWoodDoor : BaseDoor
{
    [Constructible]
    public DarkWoodDoor(DoorFacing facing) : base(
        0x6A5 + 2 * (int)facing,
        0x6A6 + 2 * (int)facing,
        0xEA,
        0xF1,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MediumWoodDoor : BaseDoor
{
    [Constructible]
    public MediumWoodDoor(DoorFacing facing) : base(
        0x6B5 + 2 * (int)facing,
        0x6B6 + 2 * (int)facing,
        0xEA,
        0xF1,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MetalDoor2 : BaseDoor
{
    [Constructible]
    public MetalDoor2(DoorFacing facing) : base(
        0x6C5 + 2 * (int)facing,
        0x6C6 + 2 * (int)facing,
        0xEC,
        0xF3,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LightWoodDoor : BaseDoor
{
    [Constructible]
    public LightWoodDoor(DoorFacing facing) : base(
        0x6D5 + 2 * (int)facing,
        0x6D6 + 2 * (int)facing,
        0xEA,
        0xF1,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class StrongWoodDoor : BaseDoor
{
    [Constructible]
    public StrongWoodDoor(DoorFacing facing) : base(
        0x6E5 + 2 * (int)facing,
        0x6E6 + 2 * (int)facing,
        0xEA,
        0xF1,
        GetOffset(facing)
    )
    {
    }
}
