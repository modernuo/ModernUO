using ModernUO.Serialization;
using Server.Engines.CannedEvil;
using Server.Regions;
using Server.Systems.FeatureFlags;
using Server.Targeting;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public abstract partial class BaseBoatDeed : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _multiId;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _offset;

    public BaseBoatDeed(int id, Point3D offset) : base(0x14F2)
    {
        if (!Core.AOS)
        {
            LootType = LootType.Newbied;
        }

        _multiId = id;
        Offset = offset;
    }

    public override double DefaultWeight => 1.0;

    public abstract BaseBoat Boat { get; }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from.AccessLevel < AccessLevel.GameMaster && (from.Map == Map.Ilshenar || from.Map == Map.Malas))
        {
            from.SendLocalizedMessage(1010567, null, 0x25); // You may not place a boat from this location.
        }
        else
        {
            if (Core.SE)
            {
                from.SendLocalizedMessage(502482); // Where do you wish to place the ship?
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502482); // Where do you wish to place the ship?
            }

            from.Target = new InternalTarget(this);
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

        if (!ContentFeatureFlags.BoatPlacement && from.AccessLevel < FeatureFlagSettings.RequiredAccessLevel)
        {
            from.SendMessage(0x22, "Boat placement is temporarily disabled.");
        }

        if (from.AccessLevel < AccessLevel.GameMaster && (map == Map.Ilshenar || map == Map.Malas))
        {
            from.SendLocalizedMessage(1043284); // A ship can not be created here.
            return;
        }

        if (from.Region.IsPartOf<HouseRegion>() || BaseBoat.FindBoatAt(from.Location, from.Map) != null)
        {
            // You may not place a ship while on another ship or inside a house.
            from.SendLocalizedMessage(1010568, null, 0x25);
            return;
        }

        var boat = Boat;

        if (boat == null)
        {
            return;
        }

        p = new Point3D(p.X - Offset.X, p.Y - Offset.Y, p.Z - Offset.Z);

        if (BaseBoat.IsValidLocation(p, map) && boat.CanFit(p, map, boat.ItemID))
        {
            Delete();

            boat.Owner = from;
            boat.Anchored = true;

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
        private readonly BaseBoatDeed m_Deed;

        public InternalTarget(BaseBoatDeed deed) : base(deed.MultiId, deed.Offset) => m_Deed = deed;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is IPoint3D ip)
            {
                if (ip is Item item)
                {
                    ip = from;
                }

                var p = new Point3D(ip);

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
                    m_Deed.OnPlacement(from, p);
                }
            }
        }
    }
}
