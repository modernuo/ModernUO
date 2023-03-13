using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class DecorativeShield : Item, IAddon, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public DecorativeShield(int itemID = 0x156C) : base(itemID) => Movable = false;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public bool FacingSouth
    {
        get
        {
            if (ItemID < 0x1582)
            {
                return (ItemID & 0x1) == 0;
            }

            return ItemID <= 0x1585;
        }
    }

    public Item Deed =>
        new DecorativeShieldDeed
        {
            IsRewardItem = _isRewardItem
        };

    public bool CouldFit(IPoint3D p, Map map) =>
        map?.CanFit(p.X, p.Y, p.Z, ItemData.Height) == true &&
        (FacingSouth && BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map) || BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map));

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Core.ML && _isRewardItem)
        {
            list.Add(1076220); // 4th Year Veteran Reward
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(Location, 2))
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house?.IsOwner(from) == true)
            {
                from.CloseGump<RewardDemolitionGump>();
                from.SendGump(new RewardDemolitionGump(this, 1049783)); // Do you wish to re-deed this decoration?
            }
            else
            {
                // You can only re-deed this decoration if you are the house owner or originally placed the decoration.
                from.SendLocalizedMessage(1049784);
            }
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }
}

[SerializationGenerator(0)]
public partial class DecorativeShieldDeed : Item, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public DecorativeShieldDeed() : base(0x14F0)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override int LabelNumber => 1049771; // deed for a decorative shield wall hanging

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076220); // 4th Year Veteran Reward
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
        {
            return;
        }

        if (IsChildOf(from.Backpack))
        {
            from.CloseGump<InternalGump>();
            from.SendGump(new InternalGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
        }
    }

    public static int GetWestItemID(int east)
    {
        return east switch
        {
            0x1582 => 0x1635,
            0x1583 => 0x1634,
            0x1584 => 0x1637,
            0x1585 => 0x1636,
            _      => east + 1
        };
    }

    private class InternalGump : Gump
    {
        public const int Start = 0x156C;
        public const int End = 0x1585;

        private readonly DecorativeShieldDeed _shield;

        public InternalGump(DecorativeShieldDeed shield) : base(150, 50)
        {
            _shield = shield;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(25, 0, 500, 230, 0xA28);

            var itemID = Start;

            for (var i = 1; i <= 2; i++)
            {
                AddPage(i);

                for (var j = 0; j < 9 - i; j++)
                {
                    AddItem(40 + j * 60, 70, itemID);
                    AddButton(60 + j * 60, 50, 0x845, 0x846, itemID);

                    if (itemID < 0x1582)
                    {
                        itemID += 2;
                    }
                    else
                    {
                        itemID += 1;
                    }
                }

                switch (i)
                {
                    case 1:
                        {
                            AddButton(455, 198, 0x8B0, 0x8B0, 0, GumpButtonType.Page, 2);
                            break;
                        }
                    case 2:
                        {
                            AddButton(70, 198, 0x8AF, 0x8AF, 0, GumpButtonType.Page, 1);
                            break;
                        }
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_shield?.Deleted != false ||
                info.ButtonID is < Start or > End ||
                ((info.ButtonID & 0x1) != 0 || info.ButtonID >= 0x1582) && info.ButtonID is < 0x1582 or > 0x1585)
            {
                return;
            }

            sender.Mobile.SendLocalizedMessage(1049780); // Where would you like to place this decoration?
            sender.Mobile.Target = new InternalTarget(_shield, info.ButtonID);
        }
    }

    private class InternalTarget : Target
    {
        private readonly int _itemID;
        private readonly DecorativeShieldDeed _shield;

        public InternalTarget(DecorativeShieldDeed shield, int itemID) : base(-1, true, TargetFlags.None)
        {
            _shield = shield;
            _itemID = itemID;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_shield?.Deleted != false)
            {
                return;
            }

            if (!_shield.IsChildOf(from.Backpack))
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

            if (p == null || map == null)
            {
                return;
            }

            var p3d = new Point3D(p);
            var id = TileData.ItemTable[_itemID & TileData.MaxItemValue];

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

            if (north && west)
            {
                from.CloseGump<FacingGump>();
                from.SendGump(new FacingGump(_shield, _itemID, p3d, house));
            }
            else if (north || west)
            {
                var shield = new DecorativeShield(west ? GetWestItemID(_itemID) : _itemID);

                house.Addons.Add(shield);

                shield.IsRewardItem = _shield.IsRewardItem;
                shield.MoveToWorld(p3d, map);

                _shield.Delete();
            }
            else
            {
                from.SendLocalizedMessage(1049781); // This decoration must be placed next to a wall.
            }
        }

        private class FacingGump : Gump
        {
            private readonly BaseHouse _house;
            private readonly int _itemID;
            private readonly Point3D _location;
            private readonly DecorativeShieldDeed _shield;

            public FacingGump(DecorativeShieldDeed shield, int itemID, Point3D location, BaseHouse house) : base(150, 50)
            {
                _shield = shield;
                _itemID = itemID;
                _location = location;
                _house = house;

                Closable = true;
                Disposable = true;
                Draggable = true;
                Resizable = false;

                AddPage(0);
                AddBackground(0, 0, 300, 150, 0xA28);

                AddItem(90, 30, GetWestItemID(itemID));
                AddItem(180, 30, itemID);

                AddButton(50, 35, 0x867, 0x869, (int)Buttons.East);
                AddButton(145, 35, 0x867, 0x869, (int)Buttons.South);
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (_shield?.Deleted != false || _house == null)
                {
                    return;
                }

                DecorativeShield shield = null;

                if (info.ButtonID == (int)Buttons.East)
                {
                    shield = new DecorativeShield(GetWestItemID(_itemID));
                }

                if (info.ButtonID == (int)Buttons.South)
                {
                    shield = new DecorativeShield(_itemID);
                }

                if (shield != null)
                {
                    _house.Addons.Add(shield);

                    shield.IsRewardItem = _shield.IsRewardItem;
                    shield.MoveToWorld(_location, sender.Mobile.Map);

                    _shield.Delete();
                }
            }

            private enum Buttons
            {
                Cancel,
                South,
                East
            }
        }
    }
}
