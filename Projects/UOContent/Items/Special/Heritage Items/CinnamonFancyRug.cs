using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class CinnamonFancyRugAddon : BaseAddon
{
    [Constructible]
    public CinnamonFancyRugAddon()
    {
        AddComponent(new LocalizedAddonComponent(0xAE3, 1076587), 1, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAE4, 1076587), -1, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xAE5, 1076587), -1, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAE6, 1076587), 1, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xAE7, 1076587), -1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0xAE8, 1076587), 0, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xAE9, 1076587), 1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0xAEA, 1076587), 0, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAEB, 1076587), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new CinnamonFancyRugDeed();
}

[SerializationGenerator(0)]
public partial class CinnamonFancyRugDeed : BaseAddonDeed
{
    [Constructible]
    public CinnamonFancyRugDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new CinnamonFancyRugAddon();
    public override int LabelNumber => 1076587; // Cinnamon fancy rug
}
