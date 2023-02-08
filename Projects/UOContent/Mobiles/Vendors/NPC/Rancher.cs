using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Rancher : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Rancher() : base("the rancher")
        {
            SetSkill(SkillName.AnimalLore, 55.0, 78.0);
            SetSkill(SkillName.AnimalTaming, 55.0, 78.0);
            SetSkill(SkillName.Herding, 64.0, 100.0);
            SetSkill(SkillName.Veterinary, 60.0, 83.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBRancher());
        }
    }
}
