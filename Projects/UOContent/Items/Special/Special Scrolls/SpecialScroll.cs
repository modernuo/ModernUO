using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    public abstract class SpecialScroll : Item
    {
        public SpecialScroll(SkillName skill, double value) : base(0x14F0)
        {
            LootType = LootType.Cursed;
            Weight = 1.0;

            Skill = skill;
            Value = value;
        }

        public SpecialScroll(Serial serial) : base(serial)
        {
        }

        /* DO NOT USE! Only used in serialization of special scrolls that originally derived from Item */

        protected bool InheritsItem { get; private set; }

        public abstract int Message { get; }
        public virtual int Title => 0;
        public abstract string DefaultTitle { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Value { get; set; }

        public virtual string GetNameLocalized() => $"#{AosSkillBonuses.GetLabel(Skill)}";

        public virtual string GetName()
        {
            var index = (int)Skill;
            var table = SkillInfo.Table;

            if (index >= 0 && index < table.Length)
            {
                return table[index].Name.ToLower();
            }

            return "???";
        }

        public virtual bool CanUse(Mobile from)
        {
            if (Deleted)
            {
                return false;
            }

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return false;
            }

            return true;
        }

        public virtual void Use(Mobile from)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!CanUse(from))
            {
                return;
            }

            from.CloseGump<InternalGump>();
            from.SendGump(new InternalGump(from, this));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write((int)Skill);
            writer.Write(Value);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Skill = (SkillName)reader.ReadInt();
                        Value = reader.ReadDouble();
                        break;
                    }
                case 0:
                    {
                        InheritsItem = true;

                        if (!(this is StatCapScroll))
                        {
                            Skill = (SkillName)reader.ReadInt();
                        }
                        else
                        {
                            Skill = SkillName.Alchemy;
                        }

                        if (this is ScrollofAlacrity)
                        {
                            Value = 0.0;
                        }
                        else if (this is StatCapScroll)
                        {
                            Value = reader.ReadInt();
                        }
                        else
                        {
                            Value = reader.ReadDouble();
                        }

                        break;
                    }
            }
        }

        public class InternalGump : Gump
        {
            private readonly Mobile m_Mobile;
            private readonly SpecialScroll m_Scroll;

            public InternalGump(Mobile mobile, SpecialScroll scroll) : base(25, 50)
            {
                m_Mobile = mobile;
                m_Scroll = scroll;

                AddPage(0);

                AddBackground(25, 10, 420, 200, 5054);

                AddImageTiled(33, 20, 401, 181, 2624);
                AddAlphaRegion(33, 20, 401, 181);

                AddHtmlLocalized(40, 48, 387, 100, m_Scroll.Message, true, true);

                AddHtmlLocalized(125, 148, 200, 20, 1049478, 0xFFFFFF); // Do you wish to use this scroll?

                AddButton(100, 172, 4005, 4007, 1);
                AddHtmlLocalized(135, 172, 120, 20, 1046362, 0xFFFFFF); // Yes

                AddButton(275, 172, 4005, 4007, 0);
                AddHtmlLocalized(310, 172, 120, 20, 1046363, 0xFFFFFF); // No

                if (m_Scroll.Title != 0)
                {
                    AddHtmlLocalized(40, 20, 260, 20, m_Scroll.Title, 0xFFFFFF);
                }
                else
                {
                    AddHtml(40, 20, 260, 20, m_Scroll.DefaultTitle);
                }

                if (m_Scroll is StatCapScroll)
                {
                    AddHtmlLocalized(310, 20, 120, 20, 1038019, 0xFFFFFF); // Power
                }
                else
                {
                    AddHtmlLocalized(310, 20, 120, 20, AosSkillBonuses.GetLabel(m_Scroll.Skill), 0xFFFFFF);
                }
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (info.ButtonID == 1)
                {
                    m_Scroll.Use(m_Mobile);
                }
            }
        }
    }
}
