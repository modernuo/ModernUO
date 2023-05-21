using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A69, 0x2A6D)]
[SerializationGenerator(0)]
public partial class CreepyPortraitComponent : AddonComponent
{
    public CreepyPortraitComponent() : base(0x2A69)
    {
    }

    public override int LabelNumber => 1074481; // Creepy portrait
    public override bool HandlesOnMovement => true;

    public override void OnDoubleClick(Mobile from)
    {
        if (Utility.InRange(Location, from.Location, 2))
        {
            Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x565, 0x566));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    public override void OnMovement(Mobile m, Point3D old)
    {
        if (m.Alive && m.Player && (m.AccessLevel == AccessLevel.Player || !m.Hidden))
        {
            if (!Utility.InRange(old, Location, 2) && Utility.InRange(m.Location, Location, 2))
            {
                if (ItemID is 0x2A69 or 0x2A6D)
                {
                    Up();
                    Timer.StartTimer(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), 2, Up);
                }
            }
            else if (Utility.InRange(old, Location, 2) && !Utility.InRange(m.Location, Location, 2))
            {
                if (ItemID is 0x2A6C or 0x2A70)
                {
                    Down();
                    Timer.StartTimer(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), 2, Down);
                }
            }
        }
    }

    private void Up()
    {
        ItemID += 1;
    }

    private void Down()
    {
        ItemID -= 1;
    }
}

[SerializationGenerator(0)]
public partial class CreepyPortraitAddon : BaseAddon
{
    [Constructible]
    public CreepyPortraitAddon()
    {
        AddComponent(new CreepyPortraitComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new CreepyPortraitDeed();
}

[SerializationGenerator(0)]
public partial class CreepyPortraitDeed : BaseAddonDeed
{
    [Constructible]
    public CreepyPortraitDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new CreepyPortraitAddon();
    public override int LabelNumber => 1074481; // Creepy portrait
}
