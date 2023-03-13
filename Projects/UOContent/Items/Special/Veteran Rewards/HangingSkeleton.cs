using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HangingSkeleton : Item, IAddon, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public HangingSkeleton(int itemID = 0x1596) : base(itemID)
    {
        LootType = LootType.Blessed;
        Movable = false;
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public bool FacingSouth => ItemID is 0x1A03 or 0x1A05 or 0x1A09 or 0x1B1E or 0x1B7F;

    public Item Deed =>
        new HangingSkeletonDeed
        {
            IsRewardItem = _isRewardItem
        };

    public bool CouldFit(IPoint3D p, Map map)
    {
        if (map?.CanFit(p.X, p.Y, p.Z, ItemData.Height) != true)
        {
            return false;
        }

        if (FacingSouth)
        {
            return BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map); // north wall
        }

        return BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map); // west wall
    }

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
        if (from.InRange(Location, 3))
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
public partial class HangingSkeletonDeed : Item, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public HangingSkeletonDeed() : base(0x14F0)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override int LabelNumber => 1049772; // deed for a hanging skeleton decoration

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

        from.CloseGump<InternalGump>();
        from.SendGump(new InternalGump(this));
    }

    public static int GetWestItemID(int south)
    {
        return south switch
        {
            0x1B1E => 0x1B1D,
            0x1B7F => 0x1B7C,
            _      => south + 1
        };
    }

    private class InternalGump : Gump
    {
        private readonly HangingSkeletonDeed _deed;

        public InternalGump(HangingSkeletonDeed skeleton) : base(100, 200)
        {
            _deed = skeleton;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(25, 0, 500, 230, 0xA28);

            AddPage(1);

            AddItem(130, 70, 0x1A03);
            AddButton(150, 50, 0x845, 0x846, 0x1A03);

            AddItem(190, 70, 0x1A05);
            AddButton(210, 50, 0x845, 0x846, 0x1A05);

            AddItem(250, 70, 0x1A09);
            AddButton(270, 50, 0x845, 0x846, 0x1A09);

            AddItem(310, 70, 0x1B1E);
            AddButton(330, 50, 0x845, 0x846, 0x1B1E);

            AddItem(370, 70, 0x1B7F);
            AddButton(390, 50, 0x845, 0x846, 0x1B7F);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_deed?.Deleted != false || info.ButtonID != 0x1A03 && info.ButtonID != 0x1A05 &&
                info.ButtonID != 0x1A09 && info.ButtonID != 0x1B1E && info.ButtonID != 0x1B7F)
            {
                return;
            }

            sender.Mobile.SendLocalizedMessage(1049780); // Where would you like to place this decoration?
            sender.Mobile.Target = new InternalTarget(_deed, info.ButtonID);
        }
    }

    private class InternalTarget : Target
    {
        private readonly int _itemID;
        private readonly HangingSkeletonDeed _deed;

        public InternalTarget(HangingSkeletonDeed deed, int itemID) : base(-1, true, TargetFlags.None)
        {
            _deed = deed;
            _itemID = itemID;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_deed?.Deleted != false)
            {
                return;
            }

            if (!_deed.IsChildOf(from.Backpack))
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
                from.SendGump(new FacingGump(_deed, _itemID, p3d, house));
            }
            else if (north || west)
            {
                var banner = new HangingSkeleton(west ? GetWestItemID(_itemID) : _itemID);

                house.Addons.Add(banner);

                banner.IsRewardItem = _deed.IsRewardItem;
                banner.MoveToWorld(p3d, map);

                _deed.Delete();
            }
            else
            {
                from.SendLocalizedMessage(1042039); // The banner must be placed next to a wall.
            }
        }

        private class FacingGump : Gump
        {
            private readonly BaseHouse _house;
            private readonly int _itemID;
            private readonly Point3D _location;
            private readonly HangingSkeletonDeed _skeleton;

            public FacingGump(HangingSkeletonDeed banner, int itemID, Point3D location, BaseHouse house) : base(150, 50)
            {
                _skeleton = banner;
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

                AddButton(50, 35, 0x868, 0x869, (int)Buttons.East);
                AddButton(145, 35, 0x868, 0x869, (int)Buttons.South);
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (_skeleton?.Deleted != false || _house == null)
                {
                    return;
                }

                HangingSkeleton banner = null;

                if (info.ButtonID == (int)Buttons.East)
                {
                    banner = new HangingSkeleton(GetWestItemID(_itemID));
                }

                if (info.ButtonID == (int)Buttons.South)
                {
                    banner = new HangingSkeleton(_itemID);
                }

                if (banner != null)
                {
                    _house.Addons.Add(banner);

                    banner.IsRewardItem = _skeleton.IsRewardItem;
                    banner.MoveToWorld(_location, sender.Mobile.Map);

                    _skeleton.Delete();
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
