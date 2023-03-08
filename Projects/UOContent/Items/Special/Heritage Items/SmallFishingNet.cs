using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1EA3, 0x1EA4)]
[SerializationGenerator(0)]
public partial class SmallFishingNetComponent : AddonComponent
{
    public SmallFishingNetComponent() : base(0x1EA3)
    {
    }

    public override int LabelNumber => 1076286; // Small Fish Net
}

[SerializationGenerator(0)]
public partial class SmallFishingNetAddon : BaseAddon
{
    [Constructible]
    public SmallFishingNetAddon()
    {
        AddComponent(new SmallFishingNetComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new SmallFishingNetDeed();
}

[SerializationGenerator(0)]
public partial class SmallFishingNetDeed : BaseAddonDeed
{
    [Constructible]
    public SmallFishingNetDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new SmallFishingNetAddon();
    public override int LabelNumber => 1076286; // Small Fish Net
}
