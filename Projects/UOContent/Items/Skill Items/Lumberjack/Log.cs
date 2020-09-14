namespace Server.Items
{
    [Flippable(0x1bdd, 0x1be0)]
    public class Log : Item, ICommodity, IAxe
    {
        private CraftResource m_Resource;

        [Constructible]
        public Log(int amount = 1) : this(CraftResource.RegularWood, amount)
        {
        }

        [Constructible]
        public Log(CraftResource resource)
            : this(resource, 1)
        {
        }

        [Constructible]
        public Log(CraftResource resource, int amount)
            : base(0x1BDD)
        {
            Stackable = true;
            Weight = 2.0;
            Amount = amount;

            m_Resource = resource;
            Hue = CraftResources.GetHue(resource);
        }

        public Log(Serial serial) : base(serial)
        {
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

        public virtual bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 0, new Board()))
            {
                return false;
            }

            return true;
        }

        int ICommodity.DescriptionNumber => CraftResources.IsStandard(m_Resource)
            ? LabelNumber
            : 1075062 + ((int)m_Resource - (int)CraftResource.RegularWood);

        bool ICommodity.IsDeedable => true;

        public override void GetProperties(ObjectPropertyList list)
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

            writer.Write(1); // version

            writer.Write((int)m_Resource);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Resource = (CraftResource)reader.ReadInt();
                        break;
                    }
            }

            if (version == 0)
            {
                m_Resource = CraftResource.RegularWood;
            }
        }

        public virtual bool TryCreateBoards(Mobile from, double skill, Item item)
        {
            if (Deleted || !from.CanSee(this))
            {
                return false;
            }

            if (from.Skills.Carpentry.Value < skill &&
                from.Skills.Lumberjacking.Value < skill)
            {
                item.Delete();
                from.SendLocalizedMessage(1072652); // You cannot work this strange and unusual wood.
                return false;
            }

            ScissorHelper(from, item, 1, false);
            return true;
        }
    }

    public class HeartwoodLog : Log
    {
        [Constructible]
        public HeartwoodLog(int amount = 1)
            : base(CraftResource.Heartwood, amount)
        {
        }

        public HeartwoodLog(Serial serial) : base(serial)
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 100, new HeartwoodBoard()))
            {
                return false;
            }

            return true;
        }
    }

    public class BloodwoodLog : Log
    {
        [Constructible]
        public BloodwoodLog(int amount = 1)
            : base(CraftResource.Bloodwood, amount)
        {
        }

        public BloodwoodLog(Serial serial)
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 100, new BloodwoodBoard()))
            {
                return false;
            }

            return true;
        }
    }

    public class FrostwoodLog : Log
    {
        [Constructible]
        public FrostwoodLog(int amount = 1)
            : base(CraftResource.Frostwood, amount)
        {
        }

        public FrostwoodLog(Serial serial)
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 100, new FrostwoodBoard()))
            {
                return false;
            }

            return true;
        }
    }

    public class OakLog : Log
    {
        [Constructible]
        public OakLog(int amount = 1)
            : base(CraftResource.OakWood, amount)
        {
        }

        public OakLog(Serial serial)
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 65, new OakBoard()))
            {
                return false;
            }

            return true;
        }
    }

    public class AshLog : Log
    {
        [Constructible]
        public AshLog(int amount = 1)
            : base(CraftResource.AshWood, amount)
        {
        }

        public AshLog(Serial serial)
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 80, new AshBoard()))
            {
                return false;
            }

            return true;
        }
    }

    public class YewLog : Log
    {
        [Constructible]
        public YewLog(int amount = 1)
            : base(CraftResource.YewWood, amount)
        {
        }

        public YewLog(Serial serial)
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 95, new YewBoard()))
            {
                return false;
            }

            return true;
        }
    }
}
