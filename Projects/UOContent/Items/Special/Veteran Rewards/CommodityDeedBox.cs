using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [Furniture]
    public class CommodityDeedBox : BaseContainer, IRewardItem
    {
        private bool m_IsRewardItem;

        [Constructible]
        public CommodityDeedBox() : base(0x9AA)
        {
            Hue = 0x47;
            Weight = 4.0;
        }

        public CommodityDeedBox(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1080523; // Commodity Deed Box
        public override int DefaultGumpID => 0x43;

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

        public static CommodityDeedBox Find(Item deed)
        {
            var parent = deed;

            while (parent != null && parent is not CommodityDeedBox)
            {
                parent = parent.Parent as Item;
            }

            return parent as CommodityDeedBox;
        }
    }
}
