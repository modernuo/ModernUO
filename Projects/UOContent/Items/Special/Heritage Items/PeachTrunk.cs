using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PeachTrunkAddon : BaseAddon
{
    [Constructible]
    public PeachTrunkAddon()
    {
        AddComponent(new LocalizedAddonComponent(0xD9C, 1076786), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new PeachTrunkDeed();
}

[SerializationGenerator(0)]
public partial class PeachTrunkDeed : BaseAddonDeed
{
    [Constructible]
    public PeachTrunkDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new PeachTrunkAddon();
    public override int LabelNumber => 1076786; // Peach Trunk
}
