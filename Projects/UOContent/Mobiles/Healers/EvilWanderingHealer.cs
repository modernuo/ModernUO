using Server.Items;

namespace Server.Mobiles
{
    public class EvilWanderingHealer : BaseHealer
    {
        [Constructible]
        public EvilWanderingHealer()
        {
            Title = Core.AOS ? "the Priest Of Mondain" : "the evil wandering healer";
            Karma = -10000;

            AddItem(new GnarledStaff());

            SetSkill(SkillName.Camping, 80.0, 100.0);
            SetSkill(SkillName.Forensics, 80.0, 100.0);
            SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
        }

        public EvilWanderingHealer(Serial serial) : base(serial)
        {
        }

        public override bool CanTeach => true;

        public override bool AlwaysMurderer => true;
        public override bool ClickTitle => false; // Do not display title in OnSingleClick

        public override bool CheckTeach(SkillName skill, Mobile from)
        {
            if (!base.CheckTeach(skill, from))
            {
                return false;
            }

            return skill is SkillName.Anatomy or SkillName.Camping or SkillName.Forensics or SkillName.Healing or SkillName.SpiritSpeak;
        }

        public override bool CheckResurrect(Mobile m)
        {
            if (Core.AOS && m.Criminal)
            {
                Say(501222); // Thou art a criminal.  I shall not resurrect thee.
                return false;
            }

            return true;
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (Utility.RandomBool())
            {
                c.DropItem(new FragmentOfAMap());
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1 && Title == "the wandering healer" && Core.AOS)
            {
                Title = "the priest of Mondain";
            }
        }
    }
}
