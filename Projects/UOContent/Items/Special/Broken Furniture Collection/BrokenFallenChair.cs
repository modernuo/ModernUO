using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xC19, 0xC1A)]
[SerializationGenerator(0)]
public partial class BrokenFallenChairComponent : AddonComponent
{
    public BrokenFallenChairComponent() : base(0xC19)
    {
    }

    public override int LabelNumber => 1076264; // Broken Fallen Chair
}

[SerializationGenerator(0)]
public partial class BrokenFallenChairAddon : BaseAddon
{
    [Constructible]
    public BrokenFallenChairAddon()
    {
        AddComponent(new BrokenFallenChairComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new BrokenFallenChairDeed();
}

[SerializationGenerator(0)]
public partial class BrokenFallenChairDeed : BaseAddonDeed
{
    [Constructible]
    public BrokenFallenChairDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BrokenFallenChairAddon();
    public override int LabelNumber => 1076264; // Broken Fallen Chair
}
