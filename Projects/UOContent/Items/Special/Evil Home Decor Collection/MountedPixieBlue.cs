using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A75, 0x2A76)]
[SerializationGenerator(0)]
public partial class MountedPixieBlueComponent : AddonComponent
{
    public MountedPixieBlueComponent() : base(0x2A75)
    {
    }

    public override int LabelNumber => 1074482; // Mounted pixie

    public override void OnDoubleClick(Mobile from)
    {
        if (Utility.InRange(Location, from.Location, 2))
        {
            Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x55C, 0x55E));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }
}

[SerializationGenerator(0)]
public partial class MountedPixieBlueAddon : BaseAddon
{
    public MountedPixieBlueAddon()
    {
        AddComponent(new MountedPixieBlueComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new MountedPixieBlueDeed();
}

[SerializationGenerator(0)]
public partial class MountedPixieBlueDeed : BaseAddonDeed
{
    [Constructible]
    public MountedPixieBlueDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new MountedPixieBlueAddon();
    public override int LabelNumber => 1074482; // Mounted pixie
}
