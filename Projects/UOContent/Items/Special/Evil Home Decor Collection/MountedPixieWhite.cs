using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A79, 0x2A7A)]
[SerializationGenerator(0)]
public partial class MountedPixieWhiteComponent : AddonComponent
{
    public MountedPixieWhiteComponent() : base(0x2A79)
    {
    }

    public override int LabelNumber => 1074482; // Mounted pixie

    public override void OnDoubleClick(Mobile from)
    {
        if (Utility.InRange(Location, from.Location, 2))
        {
            Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x562, 0x564));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }
}

[SerializationGenerator(0)]
public partial class MountedPixieWhiteAddon : BaseAddon
{
    public MountedPixieWhiteAddon()
    {
        AddComponent(new MountedPixieWhiteComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new MountedPixieWhiteDeed();
}

[SerializationGenerator(0)]
public partial class MountedPixieWhiteDeed : BaseAddonDeed
{
    [Constructible]
    public MountedPixieWhiteDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new MountedPixieWhiteAddon();
    public override int LabelNumber => 1074482; // Mounted pixie
}
