using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xC17, 0xC18)]
[SerializationGenerator(0)]
public partial class BrokenCoveredChairComponent : AddonComponent
{
    public BrokenCoveredChairComponent() : base(0xC17)
    {
    }

    public override int LabelNumber => 1076257; // Broken Covered Chair
}

[SerializationGenerator(0)]
public partial class BrokenCoveredChairAddon : BaseAddon
{
    [Constructible]
    public BrokenCoveredChairAddon()
    {
        AddComponent(new BrokenCoveredChairComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new BrokenCoveredChairDeed();
}

[SerializationGenerator(0)]
public partial class BrokenCoveredChairDeed : BaseAddonDeed
{
    [Constructible]
    public BrokenCoveredChairDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BrokenCoveredChairAddon();
    public override int LabelNumber => 1076257; // Broken Covered Chair
}
