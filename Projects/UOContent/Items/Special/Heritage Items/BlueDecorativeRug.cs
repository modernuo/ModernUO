using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BlueDecorativeRugAddon : BaseAddon
{
    [Constructible]
    public BlueDecorativeRugAddon()
    {
        AddComponent(new LocalizedAddonComponent(0xAD2, 1076589), 1, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAD3, 1076589), -1, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xAD4, 1076589), -1, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAD5, 1076589), 1, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xAD6, 1076589), -1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0xAD7, 1076589), 0, -1, 0);
        AddComponent(new LocalizedAddonComponent(0xAD8, 1076589), 1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0xAD9, 1076589), 0, 1, 0);
        AddComponent(new LocalizedAddonComponent(0xAD1, 1076589), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new BlueDecorativeRugDeed();
}

[SerializationGenerator(0)]
public partial class BlueDecorativeRugDeed : BaseAddonDeed
{
    [Constructible]
    public BlueDecorativeRugDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BlueDecorativeRugAddon();
    public override int LabelNumber => 1076589; // Blue decorative rug
}
