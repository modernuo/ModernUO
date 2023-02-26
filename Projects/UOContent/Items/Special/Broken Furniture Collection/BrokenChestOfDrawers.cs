using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xC24, 0xC25)]
[SerializationGenerator(0)]
public partial class BrokenChestOfDrawersComponent : AddonComponent
{
    public BrokenChestOfDrawersComponent() : base(0xC24)
    {
    }

    public override int LabelNumber => 1076261; // Broken Chest of Drawers
}

[SerializationGenerator(0)]
public partial class BrokenChestOfDrawersAddon : BaseAddon
{
    [Constructible]
    public BrokenChestOfDrawersAddon()
    {
        AddComponent(new BrokenChestOfDrawersComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new BrokenChestOfDrawersDeed();
}

[SerializationGenerator(0)]
public partial class BrokenChestOfDrawersDeed : BaseAddonDeed
{
    [Constructible]
    public BrokenChestOfDrawersDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BrokenChestOfDrawersAddon();
    public override int LabelNumber => 1076261; // Broken Chest of Drawers
}
