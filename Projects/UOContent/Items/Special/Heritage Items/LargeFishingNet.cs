using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x3D8E, 0x3D8F)]
[SerializationGenerator(0)]
public partial class LargeFishingNetComponent : AddonComponent
{
    public LargeFishingNetComponent() : base(0x3D8E)
    {
    }

    public override int LabelNumber => 1076285; // Large Fish Net
}

[SerializationGenerator(0)]
public partial class LargeFishingNetAddon : BaseAddon
{
    [Constructible]
    public LargeFishingNetAddon()
    {
        AddComponent(new LargeFishingNetComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new LargeFishingNetDeed();
}

[SerializationGenerator(0)]
public partial class LargeFishingNetDeed : BaseAddonDeed
{
    [Constructible]
    public LargeFishingNetDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new LargeFishingNetAddon();
    public override int LabelNumber => 1076285; // Large Fish Net
}
