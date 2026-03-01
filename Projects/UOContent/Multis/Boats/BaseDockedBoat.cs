using ModernUO.Serialization;
using Server.Engines.CannedEvil;
using Server.Regions;
using Server.Systems.FeatureFlags;
using Server.Targeting;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public abstract partial class BaseDockedBoat : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _multiId;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _offset;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _shipName;

    public BaseDockedBoat(int id, Point3D offset, BaseBoat boat) : base(0x14F4)
    {
        LootType = LootType.Blessed;

        _multiId = id;
        _offset = offset;

        _shipName = boat.ShipName;
    }

    public override double DefaultWeight => 1.0;

    public abstract BaseBoat Boat { get; }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else
        {
            from.SendLocalizedMessage(502482); // Where do you wish to place the ship?

            from.Target = new InternalTarget(this);
        }
    }

    public override void AddNameProperty(IPropertyList list)
    {
        if (_shipName != null)
        {
            list.Add(_shipName);
        }
        else
        {
            base.AddNameProperty(list);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        if (_shipName != null)
        {
            LabelTo(from, _shipName);
        }
        else
        {
            base.OnSingleClick(from);
        }
    }

    public void OnPlacement(Mobile from, Point3D p)
    {
        if (Deleted)
        {
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var map = from.Map;

        if (map == null)
        {
            return;
        }

        var boat = Boat;

        if (boat == null)
        {
            return;
        }

        if (!ContentFeatureFlags.BoatPlacement && from.AccessLevel < FeatureFlagSettings.RequiredAccessLevel)
        {
            from.SendMessage(0x22, "Boat placement is temporarily disabled.");
            return;
        }

        p = new Point3D(p.X - Offset.X, p.Y - Offset.Y, p.Z - Offset.Z);

        if (BaseBoat.IsValidLocation(p, map) && boat.CanFit(p, map, boat.ItemID) && map != Map.Ilshenar &&
            map != Map.Malas)
        {
            Delete();

            boat.Owner = from;
            boat.Anchored = true;
            boat.ShipName = _shipName;

            var keyValue = boat.CreateKeys(from);

            if (boat.PPlank != null)
            {
                boat.PPlank.KeyValue = keyValue;
            }

            if (boat.SPlank != null)
            {
                boat.SPlank.KeyValue = keyValue;
            }

            boat.MoveToWorld(p, map);
        }
        else
        {
            boat.Delete();
            from.SendLocalizedMessage(1043284); // A ship can not be created here.
        }
    }

    private class InternalTarget : MultiTarget
    {
        private readonly BaseDockedBoat m_Model;

        public InternalTarget(BaseDockedBoat model) : base(model.MultiId, model.Offset) => m_Model = model;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is not IPoint3D ip)
            {
                return;
            }

            var p = ip switch
            {
                Item item => item.GetWorldTop(),
                Mobile m  => m.Location,
                _         => new Point3D(ip)
            };

            var region = Region.Find(p, from.Map);

            if (region.IsPartOf<DungeonRegion>())
            {
                from.SendLocalizedMessage(502488); // You can not place a ship inside a dungeon.
            }
            else if (region.IsPartOf<HouseRegion>() || region.IsPartOf<ChampionSpawnRegion>())
            {
                from.SendLocalizedMessage(1042549); // A boat may not be placed in this area.
            }
            else
            {
                m_Model.OnPlacement(from, p);
            }
        }
    }
}
