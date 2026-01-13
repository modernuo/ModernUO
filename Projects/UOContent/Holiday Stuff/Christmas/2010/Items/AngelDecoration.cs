using ModernUO.Serialization;

namespace Server.Items.Holiday;

[Flippable(0x46FA, 0x46FB)]
[TypeAlias("Server.Items.AngelDecoration")]
[SerializationGenerator(0, false)]
public partial class AngelDecoration : Item
{
    public AngelDecoration() : base(0x46FA) => LootType = LootType.Blessed;

    public override double DefaultWeight => 30.0;
}
