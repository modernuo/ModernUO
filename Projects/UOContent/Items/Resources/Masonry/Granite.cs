namespace Server.Items
{
    public abstract class BaseGranite : Item
    {
        private CraftResource m_Resource;

        public BaseGranite(CraftResource resource) : base(0x1779)
        {
            Hue = CraftResources.GetHue(resource);
            Stackable = Core.ML;

            m_Resource = resource;
        }

        public BaseGranite(Serial serial) : base(serial)
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

        public override double DefaultWeight => Core.ML ? 1.0 : 10.0;

        public override int LabelNumber => 1044607; // high quality granite

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
                case 0:
                    {
                        m_Resource = (CraftResource)reader.ReadInt();
                        break;
                    }
            }

            if (version < 1)
            {
                Stackable = Core.ML;
            }
        }

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
    }

    public class Granite : BaseGranite
    {
        [Constructible]
        public Granite() : base(CraftResource.Iron)
        {
        }

        public Granite(Serial serial) : base(serial)
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

    public class DullCopperGranite : BaseGranite
    {
        [Constructible]
        public DullCopperGranite() : base(CraftResource.DullCopper)
        {
        }

        public DullCopperGranite(Serial serial) : base(serial)
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

    public class ShadowIronGranite : BaseGranite
    {
        [Constructible]
        public ShadowIronGranite() : base(CraftResource.ShadowIron)
        {
        }

        public ShadowIronGranite(Serial serial) : base(serial)
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

    public class CopperGranite : BaseGranite
    {
        [Constructible]
        public CopperGranite() : base(CraftResource.Copper)
        {
        }

        public CopperGranite(Serial serial) : base(serial)
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

    public class BronzeGranite : BaseGranite
    {
        [Constructible]
        public BronzeGranite() : base(CraftResource.Bronze)
        {
        }

        public BronzeGranite(Serial serial) : base(serial)
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

    public class GoldGranite : BaseGranite
    {
        [Constructible]
        public GoldGranite() : base(CraftResource.Gold)
        {
        }

        public GoldGranite(Serial serial) : base(serial)
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

    public class AgapiteGranite : BaseGranite
    {
        [Constructible]
        public AgapiteGranite() : base(CraftResource.Agapite)
        {
        }

        public AgapiteGranite(Serial serial) : base(serial)
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

    public class VeriteGranite : BaseGranite
    {
        [Constructible]
        public VeriteGranite() : base(CraftResource.Verite)
        {
        }

        public VeriteGranite(Serial serial) : base(serial)
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

    public class ValoriteGranite : BaseGranite
    {
        [Constructible]
        public ValoriteGranite() : base(CraftResource.Valorite)
        {
        }

        public ValoriteGranite(Serial serial) : base(serial)
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
