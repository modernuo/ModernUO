using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Fireflies : Item, IAddon
{
    [Constructible]
    public Fireflies(int itemID = 0x1596) : base(itemID)
    {
        LootType = LootType.Blessed;
        Movable = false;
    }

    public override int LabelNumber => 1150061;

    public bool FacingSouth => ItemID == 0x2336;

    public Item Deed => new FirefliesDeed();

    public bool CouldFit(IPoint3D p, Map map)
    {
        if (map?.CanFit(p.X, p.Y, p.Z, ItemData.Height) != true)
        {
            return false;
        }

        return FacingSouth
            ? BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map)
            : BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(Location, 3))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house?.IsOwner(from) == true)
        {
            // Do you wish to re-deed this decoration?
            from.SendGump(new RewardDemolitionGump(this, 1049783));
        }
        else
        {
            // You can only re-deed this decoration if you are the house owner or originally placed the decoration.
            from.SendLocalizedMessage(1049784);
        }
    }
}

[SerializationGenerator(0)]
public partial class FirefliesDeed : Item
{
    [Constructible]
    public FirefliesDeed() : base(0x14F0) => LootType = LootType.Blessed;

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1150061;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
            return;
        }

        var house = BaseHouse.FindHouseAt(from);

        if (house?.IsOwner(from) != true)
        {
            from.SendLocalizedMessage(502092); // You must be in your house to do this.
            return;
        }

        from.SendGump(new FacingGump(this));
    }

    private class FacingGump : StaticGump<FacingGump>
    {
        private readonly FirefliesDeed _deed;

        public override bool Singleton => true;

        public FacingGump(FirefliesDeed deed) : base(150, 50) => _deed = deed;

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddBackground(0, 0, 300, 150, 0xA28);
            builder.AddPage();

            builder.AddItem(90, 30, 0x2332);
            builder.AddItem(180, 30, 0x2336);

            builder.AddButton(50, 35, 0x868, 0x869, (int)Buttons.East);
            builder.AddButton(145, 35, 0x868, 0x869, (int)Buttons.South);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (info.ButtonID == (int)Buttons.Cancel)
            {
                return;
            }

            int itemId = info.ButtonID switch
            {
                (int)Buttons.East  => 0x2332,
                (int)Buttons.South => 0x2336,
                _                  => 0
            };

            if (itemId == 0)
            {
                return;
            }

            sender.Mobile.Target = new InternalTarget(_deed, itemId);
        }

        private enum Buttons
        {
            Cancel,
            South,
            East
        }
    }

    private class InternalTarget : Target
    {
        private readonly FirefliesDeed _firefliesDeed;
        private readonly int _itemId;

        public InternalTarget(FirefliesDeed _deed, int itemId) : base(-1, true, TargetFlags.None)
        {
            _firefliesDeed = _deed;
            _itemId = itemId;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_firefliesDeed?.Deleted != false)
            {
                return;
            }

            if (!_firefliesDeed.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
                return;
            }

            var house = BaseHouse.FindHouseAt(from);

            if (house?.IsOwner(from) != true)
            {
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
                return;
            }

            var p = targeted as IPoint3D;
            var map = from.Map;

            if (p == null || map == null || map == Map.Internal)
            {
                return;
            }

            var p3d = new Point3D(p);
            var id = TileData.ItemTable[_itemId & TileData.MaxItemValue];

            if (!map.CanFit(p3d, id.Height))
            {
                from.SendLocalizedMessage(500269); // You cannot build that there.
                return;
            }

            house = BaseHouse.FindHouseAt(p3d, map, id.Height);

            if (house?.IsOwner(from) != true)
            {
                from.SendLocalizedMessage(1042036); // That location is not in your house.
                return;
            }

            var north = BaseAddon.IsWall(p3d.X, p3d.Y - 1, p3d.Z, map);
            var west = BaseAddon.IsWall(p3d.X - 1, p3d.Y, p3d.Z, map);

            if ((_itemId != 0x2336 || !north) && (_itemId != 0x2332 || !west))
            {
                from.SendLocalizedMessage(1150065); // Holiday fireflies must be placed next to a wall.
                return;
            }

            foreach (Fireflies fireflies in Map.Malas.GetItemsAt<Fireflies>(p3d))
            {
                if (fireflies.Z == p3d.Z)
                {
                    from.SendLocalizedMessage(1150065); // Holiday fireflies must be placed next to a wall.
                    return;
                }
            }

            var flies = new Fireflies(_itemId);
            house.Addons.Add(flies);
            flies.MoveToWorld(p3d, from.Map);
            _firefliesDeed.Delete();
        }
    }
}
