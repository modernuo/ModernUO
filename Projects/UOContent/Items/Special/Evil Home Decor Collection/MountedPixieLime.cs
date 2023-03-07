using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A77, 0x2A78)]
[SerializationGenerator(0)]
public partial class MountedPixieLimeComponent : AddonComponent
{
    public MountedPixieLimeComponent() : base(0x2A77)
    {
    }

    public override int LabelNumber => 1074482; // Mounted pixie

    public override void OnDoubleClick(Mobile from)
    {
        if (Utility.InRange(Location, from.Location, 2))
        {
            Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x55F, 0x561));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }
}

[SerializationGenerator(0)]
public partial class MountedPixieLimeAddon : BaseAddon
{
    public MountedPixieLimeAddon()
    {
        AddComponent(new MountedPixieLimeComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new MountedPixieLimeDeed();
}

[SerializationGenerator(0)]
public partial class MountedPixieLimeDeed : BaseAddonDeed
{
    [Constructible]
    public MountedPixieLimeDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new MountedPixieLimeAddon();
    public override int LabelNumber => 1074482; // Mounted pixie
}
