using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A5D, 0x2A61)]
[SerializationGenerator(0)]
public partial class DisturbingPortraitComponent : AddonComponent
{
    private Timer _timer;

    public DisturbingPortraitComponent() : base(0x2A5D)
    {
        StartTimer();
    }

    public override int LabelNumber => 1074479; // Disturbing portrait

    private void StartTimer()
    {
        _timer = Timer.DelayCall(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3), Change);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Utility.InRange(Location, from.Location, 2))
        {
            Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x567, 0x568));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _timer?.Stop();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        StartTimer();
    }

    private void Change()
    {
        ItemID = ItemID < 0x2A61 ? Utility.RandomMinMax(0x2A5D, 0x2A60) : Utility.RandomMinMax(0x2A61, 0x2A64);
    }
}

[SerializationGenerator(0)]
public partial class DisturbingPortraitAddon : BaseAddon
{
    [Constructible]
    public DisturbingPortraitAddon()
    {
        AddComponent(new DisturbingPortraitComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new DisturbingPortraitDeed();
}

[SerializationGenerator(0)]
public partial class DisturbingPortraitDeed : BaseAddonDeed
{
    [Constructible]
    public DisturbingPortraitDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new DisturbingPortraitAddon();
    public override int LabelNumber => 1074479; // Disturbing portrait
}
