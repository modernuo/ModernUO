using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A58, 0x2A59)]
[SerializationGenerator(0)]
public partial class BoneThroneComponent : AddonComponent
{
    public BoneThroneComponent() : base(0x2A58)
    {
    }

    public override int LabelNumber => 1074476; // Bone throne

    public override bool OnMoveOver(Mobile m)
    {
        var allow = base.OnMoveOver(m);

        if (allow && m.Alive && m.Player && (m.AccessLevel == AccessLevel.Player || !m.Hidden))
        {
            Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x54B, 0x54D));
        }

        return allow;
    }
}

[SerializationGenerator(0)]
public partial class BoneThroneAddon : BaseAddon
{
    [Constructible]
    public BoneThroneAddon()
    {
        AddComponent(new BoneThroneComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new BoneThroneDeed();
}

[SerializationGenerator(0)]
public partial class BoneThroneDeed : BaseAddonDeed
{
    [Constructible]
    public BoneThroneDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BoneThroneAddon();
    public override int LabelNumber => 1074476; // Bone throne
}
