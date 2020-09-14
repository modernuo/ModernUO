namespace Server.Items
{
    public abstract class BaseIngot : Item, ICommodity
    {
        private CraftResource m_Resource;

        public BaseIngot(CraftResource resource, int amount = 1) : base(0x1BF2)
        {
            Stackable = true;
            Amount = amount;
            Hue = CraftResources.GetHue(resource);

            m_Resource = resource;
        }

        public BaseIngot(Serial serial) : base(serial)
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

        public override double DefaultWeight => 0.1;

        public override int LabelNumber
        {
            get
            {
                if (m_Resource >= CraftResource.DullCopper && m_Resource <= CraftResource.Valorite)
                {
                    return 1042684 + (m_Resource - CraftResource.DullCopper);
                }

                return 1042692;
            }
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;

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
                case 0:
                    {
                        var info = reader.ReadInt() switch
                        {
                            0 => OreInfo.Iron,
                            1 => OreInfo.DullCopper,
                            2 => OreInfo.ShadowIron,
                            3 => OreInfo.Copper,
                            4 => OreInfo.Bronze,
                            5 => OreInfo.Gold,
                            6 => OreInfo.Agapite,
                            7 => OreInfo.Verite,
                            8 => OreInfo.Valorite,
                            _ => null
                        };

                        m_Resource = CraftResources.GetFromOreInfo(info);
                        break;
                    }
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (Amount > 1)
            {
                list.Add(1050039, "{0}\t#{1}", Amount, 1027154); // ~1_NUMBER~ ~2_ITEMNAME~
            }
            else
            {
                list.Add(1027154); // ingots
            }
        }

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
    }

    [Flippable(0x1BF2, 0x1BEF)]
    public class IronIngot : BaseIngot
    {
        [Constructible]
        public IronIngot(int amount = 1) : base(CraftResource.Iron, amount)
        {
        }

        public IronIngot(Serial serial) : base(serial)
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

    [Flippable(0x1BF2, 0x1BEF)]
    public class DullCopperIngot : BaseIngot
    {
        [Constructible]
        public DullCopperIngot(int amount = 1) : base(CraftResource.DullCopper, amount)
        {
        }

        public DullCopperIngot(Serial serial) : base(serial)
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

    [Flippable(0x1BF2, 0x1BEF)]
    public class ShadowIronIngot : BaseIngot
    {
        [Constructible]
        public ShadowIronIngot(int amount = 1) : base(CraftResource.ShadowIron, amount)
        {
        }

        public ShadowIronIngot(Serial serial) : base(serial)
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

    [Flippable(0x1BF2, 0x1BEF)]
    public class CopperIngot : BaseIngot
    {
        [Constructible]
        public CopperIngot(int amount = 1) : base(CraftResource.Copper, amount)
        {
        }

        public CopperIngot(Serial serial) : base(serial)
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

    [Flippable(0x1BF2, 0x1BEF)]
    public class BronzeIngot : BaseIngot
    {
        [Constructible]
        public BronzeIngot(int amount = 1) : base(CraftResource.Bronze, amount)
        {
        }

        public BronzeIngot(Serial serial) : base(serial)
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

    [Flippable(0x1BF2, 0x1BEF)]
    public class GoldIngot : BaseIngot
    {
        [Constructible]
        public GoldIngot(int amount = 1) : base(CraftResource.Gold, amount)
        {
        }

        public GoldIngot(Serial serial) : base(serial)
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

    [Flippable(0x1BF2, 0x1BEF)]
    public class AgapiteIngot : BaseIngot
    {
        [Constructible]
        public AgapiteIngot(int amount = 1) : base(CraftResource.Agapite, amount)
        {
        }

        public AgapiteIngot(Serial serial) : base(serial)
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

    [Flippable(0x1BF2, 0x1BEF)]
    public class VeriteIngot : BaseIngot
    {
        [Constructible]
        public VeriteIngot(int amount = 1) : base(CraftResource.Verite, amount)
        {
        }

        public VeriteIngot(Serial serial) : base(serial)
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

    [Flippable(0x1BF2, 0x1BEF)]
    public class ValoriteIngot : BaseIngot
    {
        [Constructible]
        public ValoriteIngot(int amount = 1) : base(CraftResource.Valorite, amount)
        {
        }

        public ValoriteIngot(Serial serial) : base(serial)
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
