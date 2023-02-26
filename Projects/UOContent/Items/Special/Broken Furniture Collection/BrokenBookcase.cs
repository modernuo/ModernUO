using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xC14, 0xC15)]
[SerializationGenerator(0)]
public partial class BrokenBookcaseComponent : AddonComponent
{
    public BrokenBookcaseComponent() : base(0xC14)
    {
    }

    public override int LabelNumber => 1076258; // Broken Bookcase
}

[SerializationGenerator(0)]
public partial class BrokenBookcaseAddon : BaseAddon
{
    [Constructible]
    public BrokenBookcaseAddon()
    {
        AddComponent(new BrokenBookcaseComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new BrokenBookcaseDeed();
}

[SerializationGenerator(0)]
public partial class BrokenBookcaseDeed : BaseAddonDeed
{
    [Constructible]
    public BrokenBookcaseDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BrokenBookcaseAddon();
    public override int LabelNumber => 1076258; // Broken Bookcase
}
