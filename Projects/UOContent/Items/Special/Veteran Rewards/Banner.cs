using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class Banner : Item, IAddon, IDyable, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public Banner(int itemID) : base(itemID)
        {
            LootType = LootType.Blessed;
            Movable = false;
        }

        public Banner(Serial serial) : base(serial)
        {
        }

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        public bool FacingSouth => (ItemID & 0x1) == 0;

        public Item Deed
        {
            get
            {
                var deed = new BannerDeed();
                deed.IsRewardItem = m_IsRewardItem;

                return deed;
            }
        }

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

            return BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map);     // west wall
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

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get => m_IsRewardItem;
            set
            {
                m_IsRewardItem = value;
                InvalidateProperties();
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Core.ML && m_IsRewardItem)
            {
                list.Add(1076218); // 2nd Year Veteran Reward
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
                    from.SendGump(new RewardDemolitionGump(this, 1018318)); // Do you wish to re-deed this banner?
                }
                else
                {
                    from.SendLocalizedMessage(
                        1018330
                    ); // You can only re-deed a banner if you placed it or you are the owner of the house.
                }
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_IsRewardItem);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_IsRewardItem = reader.ReadBool();
        }
    }

    public class BannerDeed : Item, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public BannerDeed() : base(0x14F0)
        {
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public BannerDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041007; // a banner deed

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get => m_IsRewardItem;
            set
            {
                m_IsRewardItem = value;
                InvalidateProperties();
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsRewardItem)
            {
                list.Add(1076218); // 2nd Year Veteran Reward
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_IsRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
            {
                return;
            }

            if (IsChildOf(from.Backpack))
            {
                var house = BaseHouse.FindHouseAt(from);

                if (house?.IsOwner(from) == true)
                {
                    from.CloseGump<InternalGump>();
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_IsRewardItem);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_IsRewardItem = reader.ReadBool();
        }

        private class InternalGump : Gump
        {
            public const int Start = 0x15AE;
            public const int End = 0x15F4;

            private readonly BannerDeed m_Banner;

            public InternalGump(BannerDeed banner) : base(100, 200)
            {
                m_Banner = banner;

                Closable = true;
                Disposable = true;
                Draggable = true;
                Resizable = false;

                AddPage(0);

                AddBackground(25, 0, 520, 230, 0xA28);
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

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (m_Banner?.Deleted != false)
                {
                    return;
                }

                var m = sender.Mobile;

                if (info.ButtonID is < Start or > End || (info.ButtonID & 0x1) != 0)
                {
                    return;
                }

                m.SendLocalizedMessage(1042037); // Where would you like to place this banner?
                m.Target = new InternalTarget(m_Banner, info.ButtonID);
            }
        }

        private class InternalTarget : Target
        {
            private readonly BannerDeed m_Banner;
            private readonly int m_ItemID;

            public InternalTarget(BannerDeed banner, int itemID) : base(-1, true, TargetFlags.None)
            {
                m_Banner = banner;
                m_ItemID = itemID;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Banner?.Deleted != false)
                {
                    return;
                }

                if (m_Banner.IsChildOf(from.Backpack))
                {
                    var house = BaseHouse.FindHouseAt(from);

                    if (house?.IsOwner(from) == true)
                    {
                        var p = targeted as IPoint3D;
                        var map = from.Map;

                        if (p == null || map == null)
                        {
                            return;
                        }

                        var p3d = new Point3D(p);
                        var id = TileData.ItemTable[m_ItemID & TileData.MaxItemValue];

                        if (map.CanFit(p3d, id.Height))
                        {
                            house = BaseHouse.FindHouseAt(p3d, map, id.Height);

                            if (house?.IsOwner(from) == true)
                            {
                                var north = BaseAddon.IsWall(p3d.X, p3d.Y - 1, p3d.Z, map);
                                var west = BaseAddon.IsWall(p3d.X - 1, p3d.Y, p3d.Z, map);

                                if (north && west)
                                {
                                    from.CloseGump<FacingGump>();
                                    from.SendGump(new FacingGump(m_Banner, m_ItemID, p3d, house));
                                }
                                else if (north || west)
                                {
                                    var banner = new Banner(m_ItemID + (west ? 0 : 1));

                                    house.Addons.Add(banner);

                                    banner.IsRewardItem = m_Banner.IsRewardItem;
                                    banner.MoveToWorld(p3d, map);

                                    m_Banner.Delete();
                                }
                                else
                                {
                                    from.SendLocalizedMessage(1042039); // The banner must be placed next to a wall.
                                }
                            }
                            else
                            {
                                from.SendLocalizedMessage(1042036); // That location is not in your house.
                            }
                        }
                        else
                        {
                            from.SendLocalizedMessage(500269); // You cannot build that there.
                        }
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

            private class FacingGump : Gump
            {
                private readonly BannerDeed m_Banner;
                private readonly BaseHouse m_House;
                private readonly int m_ItemID;
                private readonly Point3D m_Location;

                public FacingGump(BannerDeed banner, int itemID, Point3D location, BaseHouse house) : base(150, 50)
                {
                    m_Banner = banner;
                    m_ItemID = itemID;
                    m_Location = location;
                    m_House = house;

                    Closable = true;
                    Disposable = true;
                    Draggable = true;
                    Resizable = false;

                    AddPage(0);

                    AddBackground(0, 0, 300, 150, 0xA28);

                    AddItem(90, 30, itemID + 1);
                    AddItem(180, 30, itemID);

                    AddButton(50, 35, 0x868, 0x869, (int)Buttons.East);
                    AddButton(145, 35, 0x868, 0x869, (int)Buttons.South);
                }

                public override void OnResponse(NetState sender, RelayInfo info)
                {
                    if (m_Banner?.Deleted != false || m_House == null)
                    {
                        return;
                    }

                    Banner banner = null;

                    if (info.ButtonID == (int)Buttons.East)
                    {
                        banner = new Banner(m_ItemID + 1);
                    }
                    else if (info.ButtonID == (int)Buttons.South)
                    {
                        banner = new Banner(m_ItemID);
                    }

                    if (banner != null)
                    {
                        m_House.Addons.Add(banner);

                        banner.IsRewardItem = m_Banner.IsRewardItem;
                        banner.MoveToWorld(m_Location, sender.Mobile.Map);

                        m_Banner.Delete();
                    }
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
}
