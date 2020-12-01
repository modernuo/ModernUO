using System.Collections.Generic;

namespace Server.Mobiles
{
    public class Tinker : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Tinker() : base("the tinker")
        {
            SetSkill(SkillName.Lockpicking, 60.0, 83.0);
            SetSkill(SkillName.RemoveTrap, 75.0, 98.0);
            SetSkill(SkillName.Tinkering, 64.0, 100.0);
        }

        public Tinker(Serial serial) : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.TinkersGuild;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBTinker());
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
