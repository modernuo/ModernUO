using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1E34, 0x1E35)]
[SerializationGenerator(0)]
public partial class ScarecrowComponent : AddonComponent
{
    public ScarecrowComponent() : base(0x1E34)
    {
    }

    public override int LabelNumber => 1076608; // Scarecrow
}

[SerializationGenerator(0)]
public partial class ScarecrowAddon : BaseAddon
{
    [Constructible]
    public ScarecrowAddon()
    {
        AddComponent(new ScarecrowComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new ScarecrowDeed();
}

[SerializationGenerator(0)]
public partial class ScarecrowDeed : BaseAddonDeed
{
    [Constructible]
    public ScarecrowDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new ScarecrowAddon();
    public override int LabelNumber => 1076608; // Scarecrow
}
