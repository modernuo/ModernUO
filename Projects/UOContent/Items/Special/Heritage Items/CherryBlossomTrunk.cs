using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class CherryBlossomTrunkAddon : BaseAddon
{
    [Constructible]
    public CherryBlossomTrunkAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x26EE, 1076784), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new CherryBlossomTrunkDeed();
}

[SerializationGenerator(0)]
public partial class CherryBlossomTrunkDeed : BaseAddonDeed
{
    [Constructible]
    public CherryBlossomTrunkDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new CherryBlossomTrunkAddon();
    public override int LabelNumber => 1076784; // Cherry Blossom Trunk
}
