using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xC12, 0xC13)]
[SerializationGenerator(0)]
public partial class BrokenArmoireComponent : AddonComponent
{
    public BrokenArmoireComponent() : base(0xC12)
    {
    }

    public override int LabelNumber => 1076262; // Broken Armoire
}

[SerializationGenerator(0)]
public partial class BrokenArmoireAddon : BaseAddon
{
    [Constructible]
    public BrokenArmoireAddon()
    {
        AddComponent(new BrokenArmoireComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new BrokenArmoireDeed();
}

[SerializationGenerator(0)]
public partial class BrokenArmoireDeed : BaseAddonDeed
{
    [Constructible]
    public BrokenArmoireDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BrokenArmoireAddon();
    public override int LabelNumber => 1076262; // Broken Armoire
}
