using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    public class RewardBrazier : Item, IRewardItem
    {
        private static readonly int[] m_Art =
        {
            0x19AA, 0x19BB
        };

        private Item m_Fire;

        private bool m_IsRewardItem;

        [Constructible]
        public RewardBrazier() : this(m_Art.RandomElement())
        {
        }

        [Constructible]
        public RewardBrazier(int itemID) : base(itemID)
        {
            LootType = LootType.Blessed;
            Weight = 10.0;
        }

        public RewardBrazier(Serial serial) : base(serial)
        {
        }

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

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

        public override void OnDelete()
        {
            TurnOff();

            base.OnDelete();
        }

        public void TurnOff()
        {
            if (m_Fire != null)
            {
                m_Fire.Delete();
                m_Fire = null;
            }
        }

        public void TurnOn()
        {
            m_Fire ??= new Item();

            m_Fire.ItemID = 0x19AB;
            m_Fire.Movable = false;
            m_Fire.MoveToWorld(new Point3D(X, Y, Z + ItemData.Height + 2), Map);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (IsLockedDown)
            {
                var house = BaseHouse.FindHouseAt(from);

                if (house?.IsCoOwner(from) == true)
                {
                    if (m_Fire != null)
                    {
                        TurnOff();
                    }
                    else
                    {
                        TurnOn();
                    }
                }
                else
                {
                    from.SendLocalizedMessage(502436); // That is not accessible.
                }
            }
            else
            {
                from.SendLocalizedMessage(502692); // This must be in a house and be locked down to work.
            }
        }

        public override void OnLocationChange(Point3D old)
        {
            m_Fire?.MoveToWorld(new Point3D(X, Y, Z + ItemData.Height), Map);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsRewardItem)
            {
                list.Add(1076222); // 6th Year Veteran Reward
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_IsRewardItem);
            writer.Write(m_Fire);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_IsRewardItem = reader.ReadBool();
            m_Fire = reader.ReadEntity<Item>();
        }
    }

    public class RewardBrazierDeed : Item, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public RewardBrazierDeed() : base(0x14F0)
        {
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public RewardBrazierDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1080527; // Brazier Deed

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

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsRewardItem)
            {
                list.Add(1076222); // 6th Year Veteran Reward
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
            private readonly RewardBrazierDeed m_Brazier;

            public InternalGump(RewardBrazierDeed brazier) : base(100, 200)
            {
                m_Brazier = brazier;

                Closable = true;
                Disposable = true;
                Draggable = true;
                Resizable = false;

                AddPage(0);
                AddBackground(0, 0, 200, 200, 2600);

                AddPage(1);
                AddLabel(45, 15, 0, "Choose a Brazier:");

                AddItem(40, 75, 0x19AA);
                AddButton(55, 50, 0x845, 0x846, 0x19AA);

                AddItem(100, 75, 0x19BB);
                AddButton(115, 50, 0x845, 0x846, 0x19BB);
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (m_Brazier?.Deleted != false)
                {
                    return;
                }

                var m = sender.Mobile;

                if (info.ButtonID != 0x19AA && info.ButtonID != 0x19BB)
                {
                    return;
                }

                var brazier = new RewardBrazier(info.ButtonID) { IsRewardItem = m_Brazier.IsRewardItem };

                if (!m.PlaceInBackpack(brazier))
                {
                    brazier.Delete();
                    m.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
                }
                else
                {
                    m_Brazier.Delete();
                }
            }
        }
    }
}
