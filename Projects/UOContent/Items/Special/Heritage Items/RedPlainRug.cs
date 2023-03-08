using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RedPlainRugAddon : BaseAddon
{
    [Constructible]
    public RedPlainRugAddon()
    {
        AddComponent(new LocalizedAddonComponent(0xAC9, 1076588), 1, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xACA, 1076588), -1, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xACB, 1076588), -1, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xACC, 1076588), 1, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xACD, 1076588), -1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0xACE, 1076588), 0, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xACF, 1076588), 1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0xAD0, 1076588), 0, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAC6, 1076588), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new RedPlainRugDeed();
}

[SerializationGenerator(0)]
public partial class RedPlainRugDeed : BaseAddonDeed
{
    [Constructible]
    public RedPlainRugDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new RedPlainRugAddon();
    public override int LabelNumber => 1076588; // Red plain rug
}
