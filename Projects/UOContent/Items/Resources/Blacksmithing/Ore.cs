using Server.Engines.Craft;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    public abstract class BaseOre : Item
    {
        private CraftResource m_Resource;

        public BaseOre(CraftResource resource, int amount = 1) : base(RandomSize())
        {
            Stackable = true;
            Amount = amount;
            Hue = CraftResources.GetHue(resource);

            m_Resource = resource;
        }

        public BaseOre(Serial serial) : base(serial)
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

        public override int LabelNumber
        {
            get
            {
                if (m_Resource >= CraftResource.DullCopper && m_Resource <= CraftResource.Valorite)
                {
                    return 1042845 + (m_Resource - CraftResource.DullCopper);
                }

                return 1042853; // iron ore;
            }
        }

        public abstract BaseIngot GetIngot();

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

        private static int RandomSize()
        {
            var rand = Utility.RandomDouble();

            if (rand < 0.12)
            {
                return 0x19B7;
            }

            if (rand < 0.18)
            {
                return 0x19B8;
            }

            if (rand < 0.25)
            {
                return 0x19BA;
            }

            return 0x19B9;
        }

        public override bool CanStackWith(Item dropped) =>
            dropped.Stackable && Stackable && dropped.GetType() == GetType() && dropped.Hue == Hue &&
            dropped.Name == Name && dropped.Amount + Amount <= 60000 && dropped != this;

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (Amount > 1)
            {
                list.Add(1050039, "{0}\t#{1}", Amount, 1026583); // ~1_NUMBER~ ~2_ITEMNAME~
            }
            else
            {
                list.Add(1026583); // ore
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

        public override void OnDoubleClick(Mobile from)
        {
            if (!Movable)
            {
                return;
            }

            if (RootParent is BaseCreature)
            {
                from.SendLocalizedMessage(500447); // That is not accessible
            }
            else if (from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(
                    501971
                ); // Select the forge on which to smelt the ore, or another pile of ore with which to combine it.
                from.Target = new InternalTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(501976); // The ore is too far away.
            }
        }

        private class InternalTarget : Target
        {
            private readonly BaseOre m_Ore;

            public InternalTarget(BaseOre ore) : base(2, false, TargetFlags.None) => m_Ore = ore;

            private bool IsForge(object obj)
            {
                if (Core.ML && obj is Mobile mobile && mobile.IsDeadBondedPet)
                {
                    return false;
                }

                if (obj.GetType().IsDefined(typeof(ForgeAttribute), false))
                {
                    return true;
                }

                var itemID = 0;

                if (obj is Item item)
                {
                    itemID = item.ItemID;
                }
                else if (obj is StaticTarget target)
                {
                    itemID = target.ItemID;
                }

                return itemID == 4017 || itemID >= 6522 && itemID <= 6569;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Ore.Deleted)
                {
                    return;
                }

                if (!from.InRange(m_Ore.GetWorldLocation(), 2))
                {
                    from.SendLocalizedMessage(501976); // The ore is too far away.
                    return;
                }

                if (targeted is BaseOre ore)
                {
                    if (!ore.Movable)
                    {
                        return;
                    }

                    if (m_Ore == ore)
                    {
                        from.SendLocalizedMessage(501972); // Select another pile or ore with which to combine this.
                        from.Target = new InternalTarget(ore);
                        return;
                    }

                    if (ore.Resource != m_Ore.Resource)
                    {
                        from.SendLocalizedMessage(501979); // You cannot combine ores of different metals.
                        return;
                    }

                    var worth = ore.Amount;

                    if (ore.ItemID == 0x19B9)
                    {
                        worth *= 8;
                    }
                    else if (ore.ItemID == 0x19B7)
                    {
                        worth *= 2;
                    }
                    else
                    {
                        worth *= 4;
                    }

                    var sourceWorth = m_Ore.Amount;

                    if (m_Ore.ItemID == 0x19B9)
                    {
                        sourceWorth *= 8;
                    }
                    else if (m_Ore.ItemID == 0x19B7)
                    {
                        sourceWorth *= 2;
                    }
                    else
                    {
                        sourceWorth *= 4;
                    }

                    worth += sourceWorth;

                    var plusWeight = 0;
                    var newID = ore.ItemID;

                    if (ore.DefaultWeight != m_Ore.DefaultWeight)
                    {
                        if (ore.ItemID == 0x19B7 || m_Ore.ItemID == 0x19B7)
                        {
                            newID = 0x19B7;
                        }
                        else if (ore.ItemID == 0x19B9)
                        {
                            newID = m_Ore.ItemID;
                            plusWeight = ore.Amount * 2;
                        }
                        else
                        {
                            plusWeight = m_Ore.Amount * 2;
                        }
                    }

                    if (ore.ItemID == 0x19B9 && worth > 120000 ||
                        (ore.ItemID == 0x19B8 || ore.ItemID == 0x19BA) && worth > 60000 ||
                        ore.ItemID == 0x19B7 && worth > 30000)
                    {
                        from.SendLocalizedMessage(1062844); // There is too much ore to combine.
                        return;
                    }

                    if (ore.RootParent is Mobile mobile &&
                        plusWeight + mobile.Backpack.TotalWeight > mobile.Backpack.MaxWeight)
                    {
                        from.SendLocalizedMessage(501978); // The weight is too great to combine in a container.
                        return;
                    }

                    ore.ItemID = newID;

                    if (ore.ItemID == 0x19B9)
                    {
                        ore.Amount = worth / 8;
                    }
                    else if (ore.ItemID == 0x19B7)
                    {
                        ore.Amount = worth / 2;
                    }
                    else
                    {
                        ore.Amount = worth / 4;
                    }

                    m_Ore.Delete();
                    return;
                }

                if (IsForge(targeted))
                {
                    var difficulty = m_Ore.Resource switch
                    {
                        CraftResource.DullCopper => 65.0,
                        CraftResource.ShadowIron => 70.0,
                        CraftResource.Copper     => 75.0,
                        CraftResource.Bronze     => 80.0,
                        CraftResource.Gold       => 85.0,
                        CraftResource.Agapite    => 90.0,
                        CraftResource.Verite     => 95.0,
                        CraftResource.Valorite   => 99.0,
                        _                        => 50.0
                    };

                    var minSkill = difficulty - 25.0;
                    var maxSkill = difficulty + 25.0;

                    if (difficulty > 50.0 && difficulty > from.Skills.Mining.Value)
                    {
                        from.SendLocalizedMessage(501986); // You have no idea how to smelt this strange ore!
                        return;
                    }

                    if (m_Ore.ItemID == 0x19B7 && m_Ore.Amount < 2)
                    {
                        from.SendLocalizedMessage(
                            501987
                        ); // There is not enough metal-bearing ore in this pile to make an ingot.
                        return;
                    }

                    if (from.CheckTargetSkill(SkillName.Mining, targeted, minSkill, maxSkill))
                    {
                        var toConsume = m_Ore.Amount;

                        if (toConsume <= 0)
                        {
                            from.SendLocalizedMessage(
                                501987
                            ); // There is not enough metal-bearing ore in this pile to make an ingot.
                        }
                        else
                        {
                            if (toConsume > 30000)
                            {
                                toConsume = 30000;
                            }

                            int ingotAmount;

                            if (m_Ore.ItemID == 0x19B7)
                            {
                                ingotAmount = toConsume / 2;

                                if (toConsume % 2 != 0)
                                {
                                    --toConsume;
                                }
                            }
                            else if (m_Ore.ItemID == 0x19B9)
                            {
                                ingotAmount = toConsume * 2;
                            }
                            else
                            {
                                ingotAmount = toConsume;
                            }

                            var ingot = m_Ore.GetIngot();
                            ingot.Amount = ingotAmount;

                            m_Ore.Consume(toConsume);
                            from.AddToBackpack(ingot);
                            // from.PlaySound( 0x57 );

                            from.SendLocalizedMessage(
                                501988
                            ); // You smelt the ore removing the impurities and put the metal in your backpack.
                        }
                    }
                    else
                    {
                        if (m_Ore.Amount < 2)
                        {
                            if (m_Ore.ItemID == 0x19B9)
                            {
                                m_Ore.ItemID = 0x19B8;
                            }
                            else
                            {
                                m_Ore.ItemID = 0x19B7;
                            }
                        }
                        else
                        {
                            m_Ore.Amount /= 2;
                        }

                        from.SendLocalizedMessage(
                            501990
                        ); // You burn away the impurities but are left with less useable metal.
                    }
                }
            }
        }
    }

    public class IronOre : BaseOre
    {
        [Constructible]
        public IronOre(int amount = 1) : base(CraftResource.Iron, amount)
        {
        }

        public IronOre(bool fixedSize) : this()
        {
            if (fixedSize)
            {
                ItemID = 0x19B8;
            }
        }

        public IronOre(Serial serial) : base(serial)
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

        public override BaseIngot GetIngot() => new IronIngot();
    }

    public class DullCopperOre : BaseOre
    {
        [Constructible]
        public DullCopperOre(int amount = 1) : base(CraftResource.DullCopper, amount)
        {
        }

        public DullCopperOre(Serial serial) : base(serial)
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

        public override BaseIngot GetIngot() => new DullCopperIngot();
    }

    public class ShadowIronOre : BaseOre
    {
        [Constructible]
        public ShadowIronOre(int amount = 1) : base(CraftResource.ShadowIron, amount)
        {
        }

        public ShadowIronOre(Serial serial) : base(serial)
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

        public override BaseIngot GetIngot() => new ShadowIronIngot();
    }

    public class CopperOre : BaseOre
    {
        [Constructible]
        public CopperOre(int amount = 1) : base(CraftResource.Copper, amount)
        {
        }

        public CopperOre(Serial serial) : base(serial)
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

        public override BaseIngot GetIngot() => new CopperIngot();
    }

    public class BronzeOre : BaseOre
    {
        [Constructible]
        public BronzeOre(int amount = 1) : base(CraftResource.Bronze, amount)
        {
        }

        public BronzeOre(Serial serial) : base(serial)
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

        public override BaseIngot GetIngot() => new BronzeIngot();
    }

    public class GoldOre : BaseOre
    {
        [Constructible]
        public GoldOre(int amount = 1) : base(CraftResource.Gold, amount)
        {
        }

        public GoldOre(Serial serial) : base(serial)
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

        public override BaseIngot GetIngot() => new GoldIngot();
    }

    public class AgapiteOre : BaseOre
    {
        [Constructible]
        public AgapiteOre(int amount = 1) : base(CraftResource.Agapite, amount)
        {
        }

        public AgapiteOre(Serial serial) : base(serial)
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

        public override BaseIngot GetIngot() => new AgapiteIngot();
    }

    public class VeriteOre : BaseOre
    {
        [Constructible]
        public VeriteOre(int amount = 1) : base(CraftResource.Verite, amount)
        {
        }

        public VeriteOre(Serial serial) : base(serial)
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

        public override BaseIngot GetIngot() => new VeriteIngot();
    }

    public class ValoriteOre : BaseOre
    {
        [Constructible]
        public ValoriteOre(int amount = 1) : base(CraftResource.Valorite, amount)
        {
        }

        public ValoriteOre(Serial serial) : base(serial)
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

        public override BaseIngot GetIngot() => new ValoriteIngot();
    }
}
