using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xC1B, 0xC1C, 0xC1E, 0xC1D)]
[SerializationGenerator(0)]
public partial class StandingBrokenChairComponent : AddonComponent
{
    public StandingBrokenChairComponent() : base(0xC1B)
    {
    }

    public override int LabelNumber => 1076259; // Standing Broken Chair
}

[SerializationGenerator(0)]
public partial class StandingBrokenChairAddon : BaseAddon
{
    [Constructible]
    public StandingBrokenChairAddon()
    {
        AddComponent(new StandingBrokenChairComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new StandingBrokenChairDeed();
}

[SerializationGenerator(0)]
public partial class StandingBrokenChairDeed : BaseAddonDeed
{
    [Constructible]
    public StandingBrokenChairDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new StandingBrokenChairAddon();
    public override int LabelNumber => 1076259; // Standing Broken Chair
}
