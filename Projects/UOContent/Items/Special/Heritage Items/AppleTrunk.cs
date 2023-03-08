using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class AppleTrunkAddon : BaseAddon
{
    [Constructible]
    public AppleTrunkAddon()
    {
        AddComponent(new LocalizedAddonComponent(0xD98, 1076785), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new AppleTrunkDeed();
}

[SerializationGenerator(0)]
public partial class AppleTrunkDeed : BaseAddonDeed
{
    [Constructible]
    public AppleTrunkDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new AppleTrunkAddon();
    public override int LabelNumber => 1076785; // Apple Trunk
}
