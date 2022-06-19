using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class HangingSkeleton : Item, IAddon, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public HangingSkeleton(int itemID = 0x1596) : base(itemID)
        {
            LootType = LootType.Blessed;
            Movable = false;
        }

        public HangingSkeleton(Serial serial) : base(serial)
        {
        }

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        public bool FacingSouth
        {
            get
            {
                if (ItemID is 0x1A03 or 0x1A05 or 0x1A09 or 0x1B1E or 0x1B7F)
                {
                    return true;
                }

                return false;
            }
        }

        public Item Deed
        {
            get
            {
                var deed = new HangingSkeletonDeed();
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
                    from.SendLocalizedMessage(
                        1049784
                    ); // You can only re-deed this decoration if you are the house owner or originally placed the decoration.
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

    public class HangingSkeletonDeed : Item, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public HangingSkeletonDeed() : base(0x14F0)
        {
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public HangingSkeletonDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1049772; // deed for a hanging skeleton decoration

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
                list.Add(1076220); // 4th Year Veteran Reward
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
            private readonly HangingSkeletonDeed m_Skeleton;

            public InternalGump(HangingSkeletonDeed skeleton) : base(100, 200)
            {
                m_Skeleton = skeleton;

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
                if (m_Skeleton?.Deleted != false || info.ButtonID != 0x1A03 && info.ButtonID != 0x1A05 &&
                    info.ButtonID != 0x1A09 && info.ButtonID != 0x1B1E && info.ButtonID != 0x1B7F)
                {
                    return;
                }

                sender.Mobile.SendLocalizedMessage(1049780); // Where would you like to place this decoration?
                sender.Mobile.Target = new InternalTarget(m_Skeleton, info.ButtonID);
            }
        }

        private class InternalTarget : Target
        {
            private readonly int m_ItemID;
            private readonly HangingSkeletonDeed m_Skeleton;

            public InternalTarget(HangingSkeletonDeed banner, int itemID) : base(-1, true, TargetFlags.None)
            {
                m_Skeleton = banner;
                m_ItemID = itemID;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Skeleton?.Deleted != false)
                {
                    return;
                }

                if (!m_Skeleton.IsChildOf(from.Backpack))
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
                var id = TileData.ItemTable[m_ItemID & TileData.MaxItemValue];

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
                    from.SendGump(new FacingGump(m_Skeleton, m_ItemID, p3d, house));
                }
                else if (north || west)
                {
                    var banner = new HangingSkeleton(west ? GetWestItemID(m_ItemID) : m_ItemID);

                    house.Addons.Add(banner);

                    banner.IsRewardItem = m_Skeleton.IsRewardItem;
                    banner.MoveToWorld(p3d, map);

                    m_Skeleton.Delete();
                }
                else
                {
                    from.SendLocalizedMessage(1042039); // The banner must be placed next to a wall.
                }
            }

            private class FacingGump : Gump
            {
                private readonly BaseHouse m_House;
                private readonly int m_ItemID;
                private readonly Point3D m_Location;
                private readonly HangingSkeletonDeed m_Skeleton;

                public FacingGump(HangingSkeletonDeed banner, int itemID, Point3D location, BaseHouse house) : base(150, 50)
                {
                    m_Skeleton = banner;
                    m_ItemID = itemID;
                    m_Location = location;
                    m_House = house;

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
                    if (m_Skeleton?.Deleted != false || m_House == null)
                    {
                        return;
                    }

                    HangingSkeleton banner = null;

                    if (info.ButtonID == (int)Buttons.East)
                    {
                        banner = new HangingSkeleton(GetWestItemID(m_ItemID));
                    }

                    if (info.ButtonID == (int)Buttons.South)
                    {
                        banner = new HangingSkeleton(m_ItemID);
                    }

                    if (banner != null)
                    {
                        m_House.Addons.Add(banner);

                        banner.IsRewardItem = m_Skeleton.IsRewardItem;
                        banner.MoveToWorld(m_Location, sender.Mobile.Map);

                        m_Skeleton.Delete();
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
}
