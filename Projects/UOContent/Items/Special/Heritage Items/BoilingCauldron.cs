using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2068, 0x207A)]
[SerializationGenerator(0)]
public partial class BoilingCauldronAddon : BaseAddonContainer
{
    [Constructible]
    public BoilingCauldronAddon() : base(0x2068)
    {
        AddComponent(new LocalizedContainerComponent(0xFAC, 1076267), 0, 0, 0);
        AddComponent(new LocalizedContainerComponent(0x970, 1076267), 0, 0, 8);
    }

    public override BaseAddonContainerDeed Deed => new BoilingCauldronDeed();
    public override int LabelNumber => 1076267; // Boiling Cauldron
    public override int DefaultGumpID => 0x9;
    public override int DefaultDropSound => 0x42;
}

[SerializationGenerator(0)]
public partial class BoilingCauldronDeed : BaseAddonContainerDeed
{
    [Constructible]
    public BoilingCauldronDeed() => LootType = LootType.Blessed;

    public override BaseAddonContainer Addon => new BoilingCauldronAddon();
    public override int LabelNumber => 1076267; // Boiling Cauldron
}
