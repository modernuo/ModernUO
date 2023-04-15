using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SecretStoneDoor1 : BaseDoor
{
    [Constructible]
    public SecretStoneDoor1(DoorFacing facing) : base(
        0xE8 + 2 * (int)facing,
        0xE9 + 2 * (int)facing,
        0xED,
        0xF4,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class SecretDungeonDoor : BaseDoor
{
    [Constructible]
    public SecretDungeonDoor(DoorFacing facing) : base(
        0x314 + 2 * (int)facing,
        0x315 + 2 * (int)facing,
        0xED,
        0xF4,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class SecretStoneDoor2 : BaseDoor
{
    [Constructible]
    public SecretStoneDoor2(DoorFacing facing) : base(
        0x324 + 2 * (int)facing,
        0x325 + 2 * (int)facing,
        0xED,
        0xF4,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class SecretWoodenDoor : BaseDoor
{
    [Constructible]
    public SecretWoodenDoor(DoorFacing facing) : base(
        0x334 + 2 * (int)facing,
        0x335 + 2 * (int)facing,
        0xED,
        0xF4,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class SecretLightWoodDoor : BaseDoor
{
    [Constructible]
    public SecretLightWoodDoor(DoorFacing facing) : base(
        0x344 + 2 * (int)facing,
        0x345 + 2 * (int)facing,
        0xED,
        0xF4,
        GetOffset(facing)
    )
    {
    }
}

[SerializationGenerator(0, false)]
public partial class SecretStoneDoor3 : BaseDoor
{
    [Constructible]
    public SecretStoneDoor3(DoorFacing facing) : base(
        0x354 + 2 * (int)facing,
        0x355 + 2 * (int)facing,
        0xED,
        0xF4,
        GetOffset(facing)
    )
    {
    }
}
