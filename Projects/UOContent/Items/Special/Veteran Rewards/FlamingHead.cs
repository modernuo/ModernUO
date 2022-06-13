using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class FlamingHead : StoneFaceTrapNoDamage, IAddon, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public FlamingHead(StoneFaceTrapType type = StoneFaceTrapType.NorthWall)
        {
            LootType = LootType.Blessed;
            Movable = false;
            Type = type;
        }

        public FlamingHead(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041266; // Flaming Head
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        public Item Deed
        {
            get
            {
                var deed = new FlamingHeadDeed();
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

            if (Type == StoneFaceTrapType.NorthWestWall)
            {
                return BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map) &&
                       BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map); // north and west wall
            }

            if (Type == StoneFaceTrapType.NorthWall)
            {
                return BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map); // north wall
            }

            if (Type == StoneFaceTrapType.WestWall)
            {
                return BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map); // west wall
            }

            return false;
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
                    from.SendGump(new RewardDemolitionGump(this, 1018329)); // Do you wish to re-deed this skull?
                }
                else
                {
                    from.SendLocalizedMessage(
                        1018328
                    ); // You can only re-deed a skull if you placed it or you are the owner of the house.
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

    public class FlamingHeadDeed : Item, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public FlamingHeadDeed() : base(0x14F0)
        {
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public FlamingHeadDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041050; // a flaming head deed

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
                    from.SendLocalizedMessage(1042264); // Where would you like to place this head?
                    from.Target = new InternalTarget(this);
                }
                else
                {
                    from.SendLocalizedMessage(502115); // You must be in your house to do this.
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

        private class InternalTarget : Target
        {
            private readonly FlamingHeadDeed m_Head;

            public InternalTarget(FlamingHeadDeed head) : base(-1, true, TargetFlags.None) => m_Head = head;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Head?.Deleted != false)
                {
                    return;
                }

                if (!m_Head.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
                    return;
                }

                var house = BaseHouse.FindHouseAt(from);

                if (house?.IsOwner(from) != true)
                {
                    from.SendLocalizedMessage(502115); // You must be in your house to do this.
                    return;
                }

                var p = targeted as IPoint3D;
                var map = from.Map;

                if (p == null || map == null)
                {
                    return;
                }

                var p3d = new Point3D(p);
                var id = TileData.ItemTable[0x10F5];

                house = BaseHouse.FindHouseAt(p3d, map, id.Height);

                if (house?.IsOwner(from) != true)
                {
                    from.SendLocalizedMessage(1042036); // That location is not in your house.
                    return;
                }

                if (!map.CanFit(p3d, id.Height))
                {
                    from.SendLocalizedMessage(1042266); // The head must be placed next to a wall.
                    return;
                }

                var north = BaseAddon.IsWall(p3d.X, p3d.Y - 1, p3d.Z, map);
                var west = BaseAddon.IsWall(p3d.X - 1, p3d.Y, p3d.Z, map);

                FlamingHead head = null;

                if (north && west)
                {
                    head = new FlamingHead(StoneFaceTrapType.NorthWestWall);
                }
                else if (north)
                {
                    head = new FlamingHead();
                }
                else if (west)
                {
                    head = new FlamingHead(StoneFaceTrapType.WestWall);
                }

                if (north || west)
                {
                    house.Addons.Add(head);

                    head.IsRewardItem = m_Head.IsRewardItem;
                    head.MoveToWorld(p3d, map);

                    m_Head.Delete();
                }
                else
                {
                    from.SendLocalizedMessage(1042266); // The head must be placed next to a wall.
                }
            }
        }
    }
}
