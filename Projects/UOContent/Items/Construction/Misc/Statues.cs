using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StatueSouth : Item
{
    [Constructible]
    public StatueSouth() : base(0x139A) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class StatueSouth2 : Item
{
    [Constructible]
    public StatueSouth2() : base(0x1227) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class StatueNorth : Item
{
    [Constructible]
    public StatueNorth() : base(0x139B) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class StatueWest : Item
{
    [Constructible]
    public StatueWest() : base(0x1226) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class StatueEast : Item
{
    [Constructible]
    public StatueEast() : base(0x139C) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class StatueEast2 : Item
{
    [Constructible]
    public StatueEast2() : base(0x1224) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class StatueSouthEast : Item
{
    [Constructible]
    public StatueSouthEast() : base(0x1225) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class BustSouth : Item
{
    [Constructible]
    public BustSouth() : base(0x12CB) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class BustEast : Item
{
    [Constructible]
    public BustEast() : base(0x12CA) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class StatuePegasus : Item
{
    [Constructible]
    public StatuePegasus() : base(0x139D) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class StatuePegasus2 : Item
{
    [Constructible]
    public StatuePegasus2() : base(0x1228) => Weight = 10;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0, false)]
public partial class SmallTowerSculpture : Item
{
    [Constructible]
    public SmallTowerSculpture() : base(0x241A) => Weight = 20.0;
}
