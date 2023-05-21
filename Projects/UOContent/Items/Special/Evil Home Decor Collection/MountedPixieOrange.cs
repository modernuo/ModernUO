using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A73, 0x2A74)]
[SerializationGenerator(0)]
public partial class MountedPixieOrangeComponent : AddonComponent
{
    public MountedPixieOrangeComponent() : base(0x2A73)
    {
    }

    public override int LabelNumber => 1074482; // Mounted pixie

    public override void OnDoubleClick(Mobile from)
    {
        if (Utility.InRange(Location, from.Location, 2))
        {
            Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x558, 0x55B));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }
}

[SerializationGenerator(0)]
public partial class MountedPixieOrangeAddon : BaseAddon
{
    public MountedPixieOrangeAddon()
    {
        AddComponent(new MountedPixieOrangeComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new MountedPixieOrangeDeed();
}

[SerializationGenerator(0)]
public partial class MountedPixieOrangeDeed : BaseAddonDeed
{
    [Constructible]
    public MountedPixieOrangeDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new MountedPixieOrangeAddon();
    public override int LabelNumber => 1074482; // Mounted pixie
}
