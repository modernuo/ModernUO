using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class DecorativeShield : Item, IAddon, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public DecorativeShield(int itemID = 0x156C) : base(itemID) => Movable = false;

        public DecorativeShield(Serial serial) : base(serial)
        {
        }

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

        public Item Deed
        {
            get
            {
                var deed = new DecorativeShieldDeed();
                deed.IsRewardItem = m_IsRewardItem;

                return deed;
            }
        }

        public bool CouldFit(IPoint3D p, Map map) =>
            map?.CanFit(p.X, p.Y, p.Z, ItemData.Height) == true && (FacingSouth
                                                                    && BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map)
                                                                    || BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map));

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

    public class DecorativeShieldDeed : Item, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public DecorativeShieldDeed() : base(0x14F0)
        {
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public DecorativeShieldDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1049771; // deed for a decorative shield wall hanging

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
                from.CloseGump<InternalGump>();
                from.SendGump(new InternalGump(this));
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

            private readonly DecorativeShieldDeed m_Shield;
            private int m_Page;

            public InternalGump(DecorativeShieldDeed shield, int page = 1) : base(150, 50)
            {
                m_Shield = shield;
                m_Page = page;

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
                            AddButton(455, 198, 0x8B0, 0x8B0, 0, GumpButtonType.Page, 2);
                            break;
                        case 2:
                            AddButton(70, 198, 0x8AF, 0x8AF, 0, GumpButtonType.Page, 1);
                            break;
                    }
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (m_Shield?.Deleted != false || info.ButtonID is < Start or > End || ((info.ButtonID & 0x1) != 0 || info.ButtonID >= 0x1582) &&
                    info.ButtonID is < 0x1582 or > 0x1585)
                {
                    return;
                }

                sender.Mobile.SendLocalizedMessage(1049780); // Where would you like to place this decoration?
                sender.Mobile.Target = new InternalTarget(m_Shield, info.ButtonID);
            }
        }

        private class InternalTarget : Target
        {
            private readonly int m_ItemID;
            private readonly DecorativeShieldDeed m_Shield;

            public InternalTarget(DecorativeShieldDeed shield, int itemID) : base(-1, true, TargetFlags.None)
            {
                m_Shield = shield;
                m_ItemID = itemID;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Shield?.Deleted != false)
                {
                    return;
                }

                if (!m_Shield.IsChildOf(from.Backpack))
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
                    from.SendGump(new FacingGump(m_Shield, m_ItemID, p3d, house));
                }
                else if (north || west)
                {
                    var shield = new DecorativeShield(west ? GetWestItemID(m_ItemID) : m_ItemID);

                    house.Addons.Add(shield);

                    shield.IsRewardItem = m_Shield.IsRewardItem;
                    shield.MoveToWorld(p3d, map);

                    m_Shield.Delete();
                }
                else
                {
                    from.SendLocalizedMessage(1049781); // This decoration must be placed next to a wall.
                }
            }

            private class FacingGump : Gump
            {
                private readonly BaseHouse m_House;
                private readonly int m_ItemID;
                private readonly Point3D m_Location;
                private readonly DecorativeShieldDeed m_Shield;

                public FacingGump(DecorativeShieldDeed shield, int itemID, Point3D location, BaseHouse house) : base(150, 50)
                {
                    m_Shield = shield;
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

                    AddButton(50, 35, 0x867, 0x869, (int)Buttons.East);
                    AddButton(145, 35, 0x867, 0x869, (int)Buttons.South);
                }

                public override void OnResponse(NetState sender, RelayInfo info)
                {
                    if (m_Shield?.Deleted != false || m_House == null)
                    {
                        return;
                    }

                    DecorativeShield shield = null;

                    if (info.ButtonID == (int)Buttons.East)
                    {
                        shield = new DecorativeShield(GetWestItemID(m_ItemID));
                    }

                    if (info.ButtonID == (int)Buttons.South)
                    {
                        shield = new DecorativeShield(m_ItemID);
                    }

                    if (shield != null)
                    {
                        m_House.Addons.Add(shield);

                        shield.IsRewardItem = m_Shield.IsRewardItem;
                        shield.MoveToWorld(m_Location, sender.Mobile.Map);

                        m_Shield.Delete();
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
