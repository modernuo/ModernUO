using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0x13E4, 0x13E3)]
    public class AncientSmithyHammer : BaseTool
    {
        private int m_Bonus;
        private SkillMod m_SkillMod;

        [Constructible]
        public AncientSmithyHammer(int bonus, int uses = 600) : base(uses, 0x13E4)
        {
            m_Bonus = bonus;
            Weight = 8.0;
            Layer = Layer.OneHanded;
            Hue = 0x482;
        }

        public AncientSmithyHammer(Serial serial) : base(serial)
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
                    m_SkillMod = new DefaultSkillMod(SkillName.Blacksmith, true, m_Bonus);
                    mobile.AddSkillMod(m_SkillMod);
                }
                else if (m_SkillMod != null)
                {
                    m_SkillMod.Value = m_Bonus;
                }
            }
        }

        public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;
        public override int LabelNumber => 1045127; // ancient smithy hammer

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (m_Bonus != 0 && parent is Mobile mobile)
            {
                m_SkillMod?.Remove();

                m_SkillMod = new DefaultSkillMod(SkillName.Blacksmith, true, m_Bonus);
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
                list.Add(1060451, $"{1042354:#}\t{m_Bonus}"); // ~1_skillname~ +~2_val~
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

                m_SkillMod = new DefaultSkillMod(SkillName.Blacksmith, true, m_Bonus);
                mobile.AddSkillMod(m_SkillMod);
            }

            if (Hue == 0)
            {
                Hue = 0x482;
            }
        }
    }
}
