using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A71, 0x2A72)]
[SerializationGenerator(0)]
public partial class MountedPixieGreenComponent : AddonComponent
{
    public MountedPixieGreenComponent() : base(0x2A71)
    {
    }

    public override int LabelNumber => 1074482; // Mounted pixie

    public override void OnDoubleClick(Mobile from)
    {
        if (Utility.InRange(Location, from.Location, 2))
        {
            Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x554, 0x557));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }
}

[SerializationGenerator(0)]
public partial class MountedPixieGreenAddon : BaseAddon
{
    public MountedPixieGreenAddon()
    {
        AddComponent(new MountedPixieGreenComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new MountedPixieGreenDeed();
}

[SerializationGenerator(0)]
public partial class MountedPixieGreenDeed : BaseAddonDeed
{
    [Constructible]
    public MountedPixieGreenDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new MountedPixieGreenAddon();
    public override int LabelNumber => 1074482; // Mounted pixie
}
