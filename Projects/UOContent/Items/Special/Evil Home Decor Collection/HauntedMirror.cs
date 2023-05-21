using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A7B, 0x2A7D)]
[SerializationGenerator(0)]
public partial class HaunterMirrorComponent : AddonComponent
{
    public HaunterMirrorComponent() : base(0x2A7B)
    {
    }

    public override int LabelNumber => 1074800; // Haunted Mirror
    public override bool HandlesOnMovement => true;

    public override void OnMovement(Mobile m, Point3D old)
    {
        base.OnMovement(m, old);

        if (!m.Alive || !m.Player || m.AccessLevel != AccessLevel.Player && m.Hidden)
        {
            return;
        }

        var inOldRange = Utility.InRange(old, Location, 2);
        var inNewRange = Utility.InRange(m.Location, Location, 2);

        if (!inOldRange && inNewRange)
        {
            if (ItemID is 0x2A7B or 0x2A7D)
            {
                Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x551, 0x553));
                ItemID += 1;
            }
        }
        else if (inOldRange && !inNewRange)
        {
            if (ItemID is 0x2A7C or 0x2A7E)
            {
                ItemID -= 1;
            }
        }
    }
}

[SerializationGenerator(0)]
public partial class HaunterMirrorAddon : BaseAddon
{
    [Constructible]
    public HaunterMirrorAddon()
    {
        AddComponent(new HaunterMirrorComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new HaunterMirrorDeed();
}

[SerializationGenerator(0)]
public partial class HaunterMirrorDeed : BaseAddonDeed
{
    [Constructible]
    public HaunterMirrorDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new HaunterMirrorAddon();
    public override int LabelNumber => 1074800; // Haunted Mirror
}
