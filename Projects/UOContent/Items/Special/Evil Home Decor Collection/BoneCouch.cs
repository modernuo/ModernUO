using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BoneCouchComponent : AddonComponent
{
    public BoneCouchComponent(int itemID) : base(itemID)
    {
    }

    public override int LabelNumber => 1074477; // Bone couch

    public override bool OnMoveOver(Mobile m)
    {
        var allow = base.OnMoveOver(m);

        if (allow && m.Alive && m.Player && (m.AccessLevel == AccessLevel.Player || !m.Hidden))
        {
            Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x547, 0x54A));
        }

        return allow;
    }
}

[FlippableAddon(Direction.South, Direction.East)]
[SerializationGenerator(0)]
public partial class BoneCouchAddon : BaseAddon
{
    [Constructible]
    public BoneCouchAddon()
    {
        Direction = Direction.South;

        AddComponent(new BoneCouchComponent(0x2A5A), 0, 0, 0);
        AddComponent(new BoneCouchComponent(0x2A5B), -1, 0, 0);
    }

    public override BaseAddonDeed Deed => new BoneCouchDeed();

    public virtual void Flip(Mobile from, Direction direction)
    {
        switch (direction)
        {
            case Direction.East:
                {
                    AddComponent(new BoneCouchComponent(0x2A80), 0, 0, 0);
                    AddComponent(new BoneCouchComponent(0x2A7F), 0, 1, 0);
                    break;
                }
            case Direction.South:
                {
                    AddComponent(new BoneCouchComponent(0x2A5A), 0, 0, 0);
                    AddComponent(new BoneCouchComponent(0x2A5B), -1, 0, 0);
                    break;
                }
        }
    }
}

[SerializationGenerator(0)]
public partial class BoneCouchDeed : BaseAddonDeed
{
    [Constructible]
    public BoneCouchDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BoneCouchAddon();
    public override int LabelNumber => 1074477; // Bone couch
}
