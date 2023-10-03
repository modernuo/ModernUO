using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BarrelLid : Item
{
    [Constructible]
    public BarrelLid() : base(0x1DB8) => Weight = 2;
}

[Flippable(0x1EB1, 0x1EB2, 0x1EB3, 0x1EB4)]
[SerializationGenerator(0, false)]
public partial class BarrelStaves : Item
{
    [Constructible]
    public BarrelStaves() : base(0x1EB1) => Weight = 1;
}

[SerializationGenerator(0, false)]
public partial class BarrelHoops : Item
{
    [Constructible]
    public BarrelHoops() : base(0x1DB7) => Weight = 5;

    public override int LabelNumber => 1011228; // Barrel hoops
}

[SerializationGenerator(0, false)]
public partial class BarrelTap : Item
{
    [Constructible]
    public BarrelTap() : base(0x1004) => Weight = 1;
}
