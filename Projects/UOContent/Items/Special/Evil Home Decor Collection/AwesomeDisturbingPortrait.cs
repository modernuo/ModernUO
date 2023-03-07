using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
[Flippable(0x2A5D, 0x2A61)]
public partial class AwesomeDisturbingPortraitComponent : AddonComponent
{
    private InternalTimer _timer;

    public AwesomeDisturbingPortraitComponent() : base(0x2A5D)
    {
        _timer = new InternalTimer(this, TimeSpan.FromSeconds(1));
        _timer.Start();
    }

    public override int LabelNumber => 1074479; // Disturbing portrait
    public bool FacingSouth => ItemID < 0x2A61;

    public override void OnDoubleClick(Mobile from)
    {
        if (Utility.InRange(Location, from.Location, 2))
        {
            Clock.GetTime(Map, X, Y, out var hours, out int _);

            if (hours is < 4 or > 20)
            {
                Effects.PlaySound(Location, Map, 0x569);
            }

            UpdateImage();
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (_timer?.Running == true)
        {
            _timer.Stop();
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        // Randomize them so they aren't all going off at the same time.
        _timer = new InternalTimer(this, TimeSpan.FromMilliseconds(8 * Utility.Random(256)));
        _timer.Start();
    }

    private void UpdateImage()
    {
        Clock.GetTime(Map, X, Y, out var hours, out int _);

        if (FacingSouth)
        {
            ItemID = hours switch
            {
                < 4  => 0x2A60,
                < 6  => 0x2A5F,
                < 8  => 0x2A5E,
                < 16 => 0x2A5D,
                < 18 => 0x2A5E,
                < 20 => 0x2A5F,
                _    => 0x2A60
            };

            return;
        }

        ItemID = hours switch
        {
            < 4  => 0x2A64,
            < 6  => 0x2A63,
            < 8  => 0x2A62,
            < 16 => 0x2A61,
            < 18 => 0x2A62,
            < 20 => 0x2A63,
            _    => 0x2A64
        };
    }

    private class InternalTimer : Timer
    {
        private readonly AwesomeDisturbingPortraitComponent _component;

        public InternalTimer(AwesomeDisturbingPortraitComponent c, TimeSpan delay)
            : base(delay, TimeSpan.FromMinutes(10)) => _component = c;

        protected override void OnTick()
        {
            if (_component?.Deleted == false)
            {
                _component.UpdateImage();
            }
        }
    }
}

[SerializationGenerator(0)]
public partial class AwesomeDisturbingPortraitAddon : BaseAddon
{
    [Constructible]
    public AwesomeDisturbingPortraitAddon()
    {
        AddComponent(new AwesomeDisturbingPortraitComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new AwesomeDisturbingPortraitDeed();
}

[SerializationGenerator(0)]
public partial class AwesomeDisturbingPortraitDeed : BaseAddonDeed
{
    [Constructible]
    public AwesomeDisturbingPortraitDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new AwesomeDisturbingPortraitAddon();
    public override int LabelNumber => 1074479; // Disturbing portrait
}
