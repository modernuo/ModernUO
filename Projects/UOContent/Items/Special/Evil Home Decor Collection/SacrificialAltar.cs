using System;
using ModernUO.Serialization;

namespace Server.Items;

[FlippableAddon(Direction.South, Direction.East)]
[SerializationGenerator(0)]
public partial class SacrificialAltarAddon : BaseAddonContainer
{
    private Timer _timer;

    [Constructible]
    public SacrificialAltarAddon() : base(0x2A9B)
    {
        Direction = Direction.South;

        AddComponent(new LocalizedContainerComponent(0x2A9A, 1074818), 1, 0, 0);
    }

    public override BaseAddonContainerDeed Deed => new SacrificialAltarDeed();
    public override int LabelNumber => 1074818; // Sacrificial Altar
    public override int DefaultMaxWeight => 0;
    public override int DefaultGumpID => 0x107;
    public override int DefaultDropSound => 0x42;

    private void InvalidateContents(Mobile from)
    {
        if (TotalItems >= 50)
        {
            SendLocalizedMessageTo(from, 501478); // The trash is full!  Emptying!
            Empty();
        }
        else
        {
            SendLocalizedMessageTo(from, 1010442); // The item will be deleted in three minutes

            if (_timer != null)
            {
                _timer.Stop();
            }
            else
            {
                _timer = new EmptyTimer(this);
            }

            _timer.Start();
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (base.OnDragDrop(from, dropped))
        {
            InvalidateContents(from);
            return true;
        }

        return false;
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (base.OnDragDropInto(from, item, p))
        {
            InvalidateContents(from);
            return true;
        }

        return false;
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        // AfterDeserialization is not synchronous (false) since Items is not filled when we deserialize.
        if (Items.Count > 0)
        {
            _timer = new EmptyTimer(this);
            _timer.Start();
        }
    }

    public virtual void Flip(Mobile from, Direction direction)
    {
        switch (direction)
        {
            case Direction.East:
                {
                    ItemID = 0x2A9C;
                    AddComponent(new LocalizedContainerComponent(0x2A9D, 1074818), 0, -1, 0);
                    break;
                }
            case Direction.South:
                {
                    ItemID = 0x2A9B;
                    AddComponent(new LocalizedContainerComponent(0x2A9A, 1074818), 1, 0, 0);
                    break;
                }
        }
    }

    public virtual void Empty()
    {
        var items = Items;

        if (items.Count > 0)
        {
            var location = Location;
            location.Z += 10;

            Effects.SendLocationEffect(location, Map, 0x3709, 10, 10, 0x356);
            Effects.PlaySound(location, Map, 0x32E);

            for (var i = items.Count - 1; i >= 0; --i)
            {
                if (i >= items.Count)
                {
                    continue;
                }

                items[i].Delete();
            }
        }

        _timer?.Stop();
        _timer = null;
    }

    private class EmptyTimer : Timer
    {
        private SacrificialAltarAddon _altar;

        public EmptyTimer(SacrificialAltarAddon altar) : base(TimeSpan.FromMinutes(3.0)) => _altar = altar;

        protected override void OnTick()
        {
            _altar.Empty();
        }
    }
}

[SerializationGenerator(0)]
public partial class SacrificialAltarDeed : BaseAddonContainerDeed
{
    [Constructible]
    public SacrificialAltarDeed() => LootType = LootType.Blessed;

    public override BaseAddonContainer Addon => new SacrificialAltarAddon();
    public override int LabelNumber => 1074818; // Sacrificial Altar
}
