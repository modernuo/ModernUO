using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2A65, 0x2A67)]
[SerializationGenerator(0)]
public partial class UnsettlingPortraitComponent : AddonComponent
{
    private Timer _timer;

    public UnsettlingPortraitComponent() : base(0x2A65)
    {
        StartTimer();
    }

    public override int LabelNumber => 1074480; // Unsettling portrait

    private void StartTimer()
    {
        _timer = Timer.DelayCall(
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(3),
            ChangeDirection
        );
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

    private void ChangeDirection()
    {
        if (ItemID == 0x2A65)
        {
            ItemID += 1;
        }
        else if (ItemID == 0x2A66)
        {
            ItemID -= 1;
        }
        else if (ItemID == 0x2A67)
        {
            ItemID += 1;
        }
        else if (ItemID == 0x2A68)
        {
            ItemID -= 1;
        }
    }
}

[SerializationGenerator(0)]
public partial class UnsettlingPortraitAddon : BaseAddon
{
    [Constructible]
    public UnsettlingPortraitAddon()
    {
        AddComponent(new UnsettlingPortraitComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new UnsettlingPortraitDeed();
}

[SerializationGenerator(0)]
public partial class UnsettlingPortraitDeed : BaseAddonDeed
{
    [Constructible]
    public UnsettlingPortraitDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new UnsettlingPortraitAddon();
    public override int LabelNumber => 1074480; // Unsettling portrait
}
