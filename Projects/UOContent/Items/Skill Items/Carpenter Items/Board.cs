namespace Server.Items
{
    [Flippable(0x1BD7, 0x1BDA)]
    public class Board : Item, ICommodity
    {
        private CraftResource m_Resource;

        [Constructible]
        public Board(int amount = 1)
            : this(CraftResource.RegularWood, amount)
        {
        }

        public Board(Serial serial)
            : base(serial)
        {
        }

        [Constructible]
        public Board(CraftResource resource, int amount = 1)
            : base(0x1BD7)
        {
            Stackable = true;
            Amount = amount;

            m_Resource = resource;
            Hue = CraftResources.GetHue(resource);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => m_Resource;
            set
            {
                m_Resource = value;
                InvalidateProperties();
            }
        }

        int ICommodity.DescriptionNumber
        {
            get
            {
                if (m_Resource >= CraftResource.OakWood && m_Resource <= CraftResource.YewWood)
                {
                    return 1075052 + ((int)m_Resource - (int)CraftResource.OakWood);
                }

                return m_Resource switch
                {
                    CraftResource.Bloodwood => 1075055,
                    CraftResource.Frostwood => 1075056,
                    CraftResource.Heartwood => 1075062, // WHY Osi.  Why?
                    _                       => LabelNumber
                };
            }
        }

        bool ICommodity.IsDeedable => true;

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (!CraftResources.IsStandard(m_Resource))
            {
                var num = CraftResources.GetLocalizationNumber(m_Resource);

                if (num > 0)
                {
                    list.Add(num);
                }
                else
                {
                    list.Add(CraftResources.GetName(m_Resource));
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3);

            writer.Write((int)m_Resource);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 3:
                case 2:
                    {
                        m_Resource = (CraftResource)reader.ReadInt();
                        break;
                    }
            }

            if (version == 0 && Weight == 0.1 || version <= 2 && Weight == 2)
            {
                Weight = -1;
            }

            if (version <= 1)
            {
                m_Resource = CraftResource.RegularWood;
            }
        }
    }

    public class HeartwoodBoard : Board
    {
        [Constructible]
        public HeartwoodBoard(int amount = 1)
            : base(CraftResource.Heartwood, amount)
        {
        }

        public HeartwoodBoard(Serial serial)
            : base(serial)
        {
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

    public class BloodwoodBoard : Board
    {
        [Constructible]
        public BloodwoodBoard(int amount = 1)
            : base(CraftResource.Bloodwood, amount)
        {
        }

        public BloodwoodBoard(Serial serial)
            : base(serial)
        {
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

    public class FrostwoodBoard : Board
    {
        [Constructible]
        public FrostwoodBoard(int amount = 1)
            : base(CraftResource.Frostwood, amount)
        {
        }

        public FrostwoodBoard(Serial serial)
            : base(serial)
        {
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

    public class OakBoard : Board
    {
        [Constructible]
        public OakBoard(int amount = 1)
            : base(CraftResource.OakWood, amount)
        {
        }

        public OakBoard(Serial serial)
            : base(serial)
        {
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

    public class AshBoard : Board
    {
        [Constructible]
        public AshBoard(int amount = 1)
            : base(CraftResource.AshWood, amount)
        {
        }

        public AshBoard(Serial serial)
            : base(serial)
        {
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

    public class YewBoard : Board
    {
        [Constructible]
        public YewBoard(int amount = 1)
            : base(CraftResource.YewWood, amount)
        {
        }

        public YewBoard(Serial serial)
            : base(serial)
        {
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
