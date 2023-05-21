using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x3D86, 0x3D87)]
[SerializationGenerator(0)]
public partial class SuitOfSilverArmorComponent : AddonComponent
{
    public SuitOfSilverArmorComponent() : base(0x3D86)
    {
    }

    public override int LabelNumber => 1076266; // Suit of Silver Armor
}

[SerializationGenerator(0)]
public partial class SuitOfSilverArmorAddon : BaseAddon
{
    [Constructible]
    public SuitOfSilverArmorAddon()
    {
        AddComponent(new SuitOfSilverArmorComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new SuitOfSilverArmorDeed();
}

[SerializationGenerator(0)]
public partial class SuitOfSilverArmorDeed : BaseAddonDeed
{
    [Constructible]
    public SuitOfSilverArmorDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new SuitOfSilverArmorAddon();
    public override int LabelNumber => 1076266; // Suit of Silver Armor
}
