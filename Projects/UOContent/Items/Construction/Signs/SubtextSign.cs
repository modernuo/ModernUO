namespace Server.Items
{
    public class SubtextSign : Sign
    {
        private string m_Subtext;

        [Constructible]
        public SubtextSign(SignType type, SignFacing facing, string subtext)
            : base(type, facing) =>
            m_Subtext = subtext;

        [Constructible]
        public SubtextSign(int itemID, string subtext)
            : base(itemID) =>
            m_Subtext = subtext;

        public SubtextSign(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Subtext
        {
            get => m_Subtext;
            set
            {
                m_Subtext = value;
                InvalidateProperties();
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (!string.IsNullOrEmpty(m_Subtext))
            {
                LabelTo(from, m_Subtext);
            }
        }

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            if (!string.IsNullOrEmpty(m_Subtext))
            {
                list.Add(m_Subtext);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(m_Subtext);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Subtext = reader.ReadString();
        }
    }
}
