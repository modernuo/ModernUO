using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BoneTableAddon : BaseAddon
{
    [Constructible]
    public BoneTableAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x2A5C, 1074478), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new BoneTableDeed();
}

[SerializationGenerator(0)]
public partial class BoneTableDeed : BaseAddonDeed
{
    [Constructible]
    public BoneTableDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BoneTableAddon();
    public override int LabelNumber => 1074478; // Bone table
}
