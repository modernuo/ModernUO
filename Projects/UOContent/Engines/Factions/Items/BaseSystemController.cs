namespace Server.Factions
{
    public abstract class BaseSystemController : Item
    {
        private int m_LabelNumber;

        public BaseSystemController(int itemID) : base(itemID)
        {
        }

        public BaseSystemController(Serial serial) : base(serial)
        {
        }

        public virtual int DefaultLabelNumber => base.LabelNumber;
        public new virtual string DefaultName => null;

        public override int LabelNumber
        {
            get
            {
                if (m_LabelNumber > 0)
                {
                    return m_LabelNumber;
                }

                return DefaultLabelNumber;
            }
        }

        public virtual void AssignName(TextDefinition name)
        {
            if (name?.Number > 0)
            {
                m_LabelNumber = name.Number;
                Name = null;
            }
            else if (name?.String != null)
            {
                m_LabelNumber = 0;
                Name = name.String;
            }
            else
            {
                m_LabelNumber = 0;
                Name = DefaultName;
            }

            InvalidateProperties();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
