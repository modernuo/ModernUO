namespace Server.Items
{
    [Flippable(0x13c6, 0x13ce)]
    public class LeatherGlovesOfMining : BaseGlovesOfMining
    {
        [Constructible]
        public LeatherGlovesOfMining(int bonus) : base(bonus, 0x13C6) => Weight = 1;

        public LeatherGlovesOfMining(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 40;

        public override int AosStrReq => 20;
        public override int OldStrReq => 10;

        public override int ArmorBase => 13;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

        public override int LabelNumber => 1045122; // leather blacksmith gloves of mining

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    [Flippable(0x13d5, 0x13dd)]
    public class StuddedGlovesOfMining : BaseGlovesOfMining
    {
        [Constructible]
        public StuddedGlovesOfMining(int bonus) : base(bonus, 0x13D5) => Weight = 2;

        public StuddedGlovesOfMining(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 35;
        public override int InitMaxHits => 45;

        public override int AosStrReq => 25;
        public override int OldStrReq => 25;

        public override int ArmorBase => 16;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override int LabelNumber => 1045123; // studded leather blacksmith gloves of mining

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    [Flippable(0x13eb, 0x13f2)]
    public class RingmailGlovesOfMining : BaseGlovesOfMining
    {
        [Constructible]
        public RingmailGlovesOfMining(int bonus) : base(bonus, 0x13EB) => Weight = 1;

        public RingmailGlovesOfMining(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 1;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 40;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 40;
        public override int OldStrReq => 20;

        public override int OldDexBonus => -1;

        public override int ArmorBase => 22;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Ringmail;

        public override int LabelNumber => 1045124; // ringmail blacksmith gloves of mining

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public abstract class BaseGlovesOfMining : BaseArmor
    {
        private int m_Bonus;
        private SkillMod m_SkillMod;

        public BaseGlovesOfMining(int bonus, int itemID) : base(itemID)
        {
            m_Bonus = bonus;

            Hue = CraftResources.GetHue(
                (CraftResource)Utility.RandomMinMax((int)CraftResource.DullCopper, (int)CraftResource.Valorite)
            );
        }

        public BaseGlovesOfMining(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Bonus
        {
            get => m_Bonus;
            set
            {
                m_Bonus = value;
                InvalidateProperties();

                if (m_Bonus == 0)
                {
                    m_SkillMod?.Remove();

                    m_SkillMod = null;
                }
                else if (m_SkillMod == null && Parent is Mobile mobile)
                {
                    m_SkillMod = new DefaultSkillMod(SkillName.Mining, true, m_Bonus);
                    mobile.AddSkillMod(m_SkillMod);
                }
                else if (m_SkillMod != null)
                {
                    m_SkillMod.Value = m_Bonus;
                }
            }
        }

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (m_Bonus != 0 && parent is Mobile mobile)
            {
                m_SkillMod?.Remove();

                m_SkillMod = new DefaultSkillMod(SkillName.Mining, true, m_Bonus);
                mobile.AddSkillMod(m_SkillMod);
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            base.OnRemoved(parent);

            m_SkillMod?.Remove();

            m_SkillMod = null;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_Bonus != 0)
            {
                list.Add(1062005, m_Bonus); // mining bonus +~1_val~
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Bonus);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Bonus = reader.ReadInt();
                        break;
                    }
            }

            if (m_Bonus != 0 && Parent is Mobile mobile)
            {
                m_SkillMod?.Remove();

                m_SkillMod = new DefaultSkillMod(SkillName.Mining, true, m_Bonus);
                mobile.AddSkillMod(m_SkillMod);
            }
        }
    }
}
