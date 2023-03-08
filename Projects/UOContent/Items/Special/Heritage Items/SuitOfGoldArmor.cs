using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x3DAA, 0x3DA9)]
[SerializationGenerator(0)]
public partial class SuitOfGoldArmorComponent : AddonComponent
{
    public SuitOfGoldArmorComponent() : base(0x3DAA)
    {
    }

    public override int LabelNumber => 1076265; // Suit of Gold Armor
}

[SerializationGenerator(0)]
public partial class SuitOfGoldArmorAddon : BaseAddon
{
    [Constructible]
    public SuitOfGoldArmorAddon()
    {
        AddComponent(new SuitOfGoldArmorComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new SuitOfGoldArmorDeed();
}

[SerializationGenerator(0)]
public partial class SuitOfGoldArmorDeed : BaseAddonDeed
{
    [Constructible]
    public SuitOfGoldArmorDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new SuitOfGoldArmorAddon();
    public override int LabelNumber => 1076265; // Suit of Gold Armor
}
