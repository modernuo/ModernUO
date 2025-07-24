using ModernUO.Serialization;

namespace Server.Items.Holiday;

[Flippable(0x4214, 0x4215)]
[TypeAlias("Server.Items.RockingHorse")]
[SerializationGenerator(0, false)]
public partial class RockingHorse : Item
{
    public RockingHorse() : base(0x4214) => LootType = LootType.Blessed;

    public override double DefaultWeight => 30.0;
}
