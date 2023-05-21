using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BluePlainRugAddon : BaseAddon
{
    [Constructible]
    public BluePlainRugAddon()
    {
        AddComponent(new LocalizedAddonComponent(0xAC2, 1076585), 1, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAC3, 1076585), -1, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xAC4, 1076585), -1, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAC5, 1076585), 1, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xAF6, 1076585), -1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0xAF7, 1076585), 0, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xAF8, 1076585), 1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0xAF9, 1076585), 0, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAC0, 1076585), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new BluePlainRugDeed();
}

[SerializationGenerator(0)]
public partial class BluePlainRugDeed : BaseAddonDeed
{
    [Constructible]
    public BluePlainRugDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BluePlainRugAddon();
    public override int LabelNumber => 1076585; // Blue plain rug
}
