using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Tinker : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Tinker() : base("the tinker")
        {
            SetSkill(SkillName.Lockpicking, 60.0, 83.0);
            SetSkill(SkillName.RemoveTrap, 75.0, 98.0);
            SetSkill(SkillName.Tinkering, 64.0, 100.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.TinkersGuild;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBTinker());
        }
    }
}
