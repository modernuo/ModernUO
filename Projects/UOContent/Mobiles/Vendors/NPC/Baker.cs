using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Baker : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Baker() : base("the baker")
        {
            SetSkill(SkillName.Cooking, 75.0, 98.0);
            SetSkill(SkillName.TasteID, 36.0, 68.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBBaker());
        }
    }
}
