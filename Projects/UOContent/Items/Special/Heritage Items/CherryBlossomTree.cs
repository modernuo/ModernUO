using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class CherryBlossomTreeAddon : BaseAddon
{
    [Constructible]
    public CherryBlossomTreeAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x26EE, 1076268), 0, 0, 0);
        AddComponent(new LocalizedAddonComponent(0x3122, 1076268), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new CherryBlossomTreeDeed();
}

[SerializationGenerator(0)]
public partial class CherryBlossomTreeDeed : BaseAddonDeed
{
    [Constructible]
    public CherryBlossomTreeDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new CherryBlossomTreeAddon();
    public override int LabelNumber => 1076268; // Cherry Blossom Tree
}
