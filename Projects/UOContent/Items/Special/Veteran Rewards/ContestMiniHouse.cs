using Server.Engines.VeteranRewards;

namespace Server.Items
{
    public class ContestMiniHouse : MiniHouseAddon
    {
        private bool m_IsRewardItem;

        [Constructible]
        public ContestMiniHouse() : base(MiniHouseType.MalasMountainPass)
        {
        }

        [Constructible]
        public ContestMiniHouse(MiniHouseType type) : base(type)
        {
        }

        public ContestMiniHouse(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                var deed = new ContestMiniHouseDeed(Type);
                deed.IsRewardItem = m_IsRewardItem;

                return deed;
            }
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

    public class ContestMiniHouseDeed : MiniHouseDeed, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public ContestMiniHouseDeed() : base(MiniHouseType.MalasMountainPass)
        {
        }

        [Constructible]
        public ContestMiniHouseDeed(MiniHouseType type) : base(type)
        {
        }

        public ContestMiniHouseDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon
        {
            get
            {
                var addon = new ContestMiniHouse(Type);
                addon.IsRewardItem = m_IsRewardItem;

                return addon;
            }
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

        public override void OnDoubleClick(Mobile from)
        {
            if (m_IsRewardItem && !RewardSystem.CheckIsUsableBy(from, this, new object[] { Type }))
            {
                return;
            }

            base.OnDoubleClick(from);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Core.ML && m_IsRewardItem)
            {
                list.Add(1076217); // 1st Year Veteran Reward
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
}
