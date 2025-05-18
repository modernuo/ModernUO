using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Banner : Item, IAddon, IDyable, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public Banner(int itemID) : base(itemID)
    {
        LootType = LootType.Blessed;
        Movable = false;
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public bool FacingSouth => (ItemID & 0x1) == 0;

    public Item Deed =>
        new BannerDeed
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

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Core.ML && _isRewardItem)
        {
            list.Add(1076218); // 2nd Year Veteran Reward
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(Location, 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house?.IsOwner(from) == true)
        {
            from.SendGump(new RewardDemolitionGump(this, 1018318)); // Do you wish to re-deed this banner?
        }
        else
        {
            // You can only re-deed a banner if you placed it or you are the owner of the house.
            from.SendLocalizedMessage(1018330);
        }
    }
}

[SerializationGenerator(0)]
public partial class BannerDeed : Item, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public BannerDeed() : base(0x14F0)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override int LabelNumber => 1041007; // a banner deed

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076218); // 2nd Year Veteran Reward
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
            var house = BaseHouse.FindHouseAt(from);

            if (house?.IsOwner(from) == true)
            {
                from.SendGump(new InternalGump(this));
            }
            else
            {
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
            }
        }
        else
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
        }
    }

    private class InternalGump : Gump
    {
        public const int Start = 0x15AE;
        public const int End = 0x15F4;

        private readonly BannerDeed _banner;

        public override bool Singleton => true;

        public InternalGump(BannerDeed banner) : base(100, 200)
        {
            _banner = banner;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(25, 0, 520, 230, 0xA28);
            // TODO: Use 1152360 - <CENTER>Choose a banner:</CENTER>
            AddLabel(70, 12, 0x3E3, "Choose a Banner:");

            var itemID = Start;

            for (var i = 1; i <= 4; i++)
            {
                AddPage(i);

                for (var j = 0; j < 8; j++, itemID += 2)
                {
                    AddItem(50 + 60 * j, 70, itemID);
                    AddButton(50 + 60 * j, 50, 0x845, 0x846, itemID);
                }

                if (i > 1)
                {
                    AddButton(75, 198, 0x8AF, 0x8AF, 0, GumpButtonType.Page, i - 1);
                }

                if (i < 4)
                {
                    AddButton(475, 198, 0x8B0, 0x8B0, 0, GumpButtonType.Page, i + 1);
                }
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_banner?.Deleted != false)
            {
                return;
            }

            var m = sender.Mobile;

            if (info.ButtonID is < Start or > End || (info.ButtonID & 0x1) != 0)
            {
                return;
            }

            m.SendLocalizedMessage(1042037); // Where would you like to place this banner?
            m.Target = new InternalTarget(_banner, info.ButtonID);
        }
    }

    private class InternalTarget : Target
    {
        private readonly BannerDeed _banner;
        private readonly int _itemID;

        public InternalTarget(BannerDeed banner, int itemID) : base(-1, true, TargetFlags.None)
        {
            _banner = banner;
            _itemID = itemID;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_banner?.Deleted != false)
            {
                return;
            }

            if (!_banner.IsChildOf(from.Backpack))
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
                from.SendGump(new FacingGump(_banner, _itemID, p3d, house));
            }
            else if (north || west)
            {
                var banner = new Banner(_itemID + (west ? 0 : 1));

                house.Addons.Add(banner);

                banner.IsRewardItem = _banner.IsRewardItem;
                banner.MoveToWorld(p3d, map);

                _banner.Delete();
            }
            else
            {
                from.SendLocalizedMessage(1042039); // The banner must be placed next to a wall.
            }
        }

        private class FacingGump : DynamicGump
        {
            private readonly BannerDeed _banner;
            private readonly BaseHouse _house;
            private readonly int _itemId;
            private readonly Point3D _location;

            public override bool Singleton => true;

            public FacingGump(BannerDeed banner, int itemID, Point3D location, BaseHouse house) : base(150, 50)
            {
                _banner = banner;
                _itemId = itemID;
                _location = location;
                _house = house;
            }

            protected override void BuildLayout(ref DynamicGumpBuilder builder)
            {
                builder.SetNoResize();

                builder.AddPage();

                builder.AddBackground(0, 0, 300, 150, 0xA28);

                builder.AddItem(90, 30, _itemId + 1);
                builder.AddItem(180, 30, _itemId);

                builder.AddButton(50, 35, 0x868, 0x869, (int)Buttons.East);
                builder.AddButton(145, 35, 0x868, 0x869, (int)Buttons.South);
            }

            public override void OnResponse(NetState sender, in RelayInfo info)
            {
                if (_banner?.Deleted != false || _house == null)
                {
                    return;
                }

                Banner banner = new Banner(_itemId + (info.ButtonID == (int)Buttons.East ? 1 : 0));
                _house.Addons.Add(banner);

                banner.IsRewardItem = _banner.IsRewardItem;
                banner.MoveToWorld(_location, sender.Mobile.Map);

                _banner.Delete();
            }

            private enum Buttons
            {
                Cancel,
                East,
                South
            }
        }
    }
}
