namespace Server.Mobiles
{
    public class EvilHealer : BaseHealer
    {
        [Constructible]
        public EvilHealer()
        {
            Title = "the healer";

            Karma = -10000;

            SetSkill(SkillName.Forensics, 80.0, 100.0);
            SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
            SetSkill(SkillName.Swords, 80.0, 100.0);
        }

        public EvilHealer(Serial serial) : base(serial)
        {
        }

        public override bool CanTeach => true;

        public override bool AlwaysMurderer => true;
        public override bool IsActiveVendor => true;

        public override bool CheckTeach(SkillName skill, Mobile from)
        {
            if (!base.CheckTeach(skill, from))
            {
                return false;
            }

            return skill == SkillName.Forensics
                   || skill == SkillName.Healing
                   || skill == SkillName.SpiritSpeak
                   || skill == SkillName.Swords;
        }

        public override void InitSBInfo()
        {
            SBInfos.Add(new SBHealer());
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
