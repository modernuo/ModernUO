using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BedOfNailsComponent : AddonComponent
{
    public BedOfNailsComponent(int itemID) : base(itemID)
    {
    }

    public override int LabelNumber => 1074801; // Bed of Nails

    public override bool OnMoveOver(Mobile m)
    {
        var allow = base.OnMoveOver(m);

        if (allow && Addon is BedOfNailsAddon addon)
        {
            addon.OnMoveOver(m);
        }

        return allow;
    }
}

[FlippableAddon(Direction.South, Direction.East)]
[SerializationGenerator(0)]
public partial class BedOfNailsAddon : BaseAddon
{
    private InternalTimer _timer;

    [Constructible]
    public BedOfNailsAddon()
    {
        Direction = Direction.South;

        AddComponent(new BedOfNailsComponent(0x2A81), 0, 0, 0);
        AddComponent(new BedOfNailsComponent(0x2A82), 0, -1, 0);
    }

    public override BaseAddonDeed Deed => new BedOfNailsDeed();

    public override bool OnMoveOver(Mobile m)
    {
        if (m.Alive && (m.AccessLevel == AccessLevel.Player || !m.Hidden))
        {
            if (m.Player)
            {
                Effects.PlaySound(
                    Location,
                    Map,
                    m.Female ? Utility.RandomMinMax(0x53B, 0x53D) : Utility.RandomMinMax(0x53E, 0x540)
                );
            }

            if (_timer?.Running != true)
            {
                (_timer = new InternalTimer(m)).Start();
            }
        }

        return true;
    }

    public virtual void Flip(Mobile from, Direction direction)
    {
        switch (direction)
        {
            case Direction.East:
                {
                    AddComponent(new BedOfNailsComponent(0x2A89), 0, 0, 0);
                    AddComponent(new BedOfNailsComponent(0x2A8A), -1, 0, 0);
                    break;
                }
            case Direction.South:
                {
                    AddComponent(new BedOfNailsComponent(0x2A81), 0, 0, 0);
                    AddComponent(new BedOfNailsComponent(0x2A82), 0, -1, 0);
                    break;
                }
        }
    }

    private class InternalTimer : Timer
    {
        private readonly Mobile _mobile;
        private Point3D _location;

        public InternalTimer(Mobile m) : base(TimeSpan.Zero, TimeSpan.FromSeconds(1), 5)
        {
            _mobile = m;
            _location = Point3D.Zero;
        }

        protected override void OnTick()
        {
            if (_mobile?.Map == null || _mobile.Deleted || !_mobile.Alive || _mobile.Map == Map.Internal)
            {
                Stop();
            }
            else if (_location != _mobile.Location)
            {
                var amount = Utility.RandomMinMax(0, 7);

                for (var i = 0; i < amount; i++)
                {
                    var x = _mobile.X + Utility.RandomMinMax(-1, 1);
                    var y = _mobile.Y + Utility.RandomMinMax(-1, 1);
                    var z = _mobile.Z;

                    if (!_mobile.Map.CanFit(x, y, z, 1, false, false))
                    {
                        z = _mobile.Map.GetAverageZ(x, y);

                        if (!_mobile.Map.CanFit(x, y, z, 1, false, false))
                        {
                            continue;
                        }
                    }

                    var blood = new Blood(Utility.RandomMinMax(0x122C, 0x122F));
                    blood.MoveToWorld(new Point3D(x, y, z), _mobile.Map);
                }

                _location = _mobile.Location;
            }
        }
    }
}

[SerializationGenerator(0)]
public partial class BedOfNailsDeed : BaseAddonDeed
{
    [Constructible]
    public BedOfNailsDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BedOfNailsAddon();
    public override int LabelNumber => 1074801; // Bed of Nails
}
