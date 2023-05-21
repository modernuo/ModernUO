using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Provisioner : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Provisioner() : base("the provisioner")
        {
            SetSkill(SkillName.Camping, 45.0, 68.0);
            SetSkill(SkillName.Tactics, 45.0, 68.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBProvisioner());

            if (IsTokunoVendor)
            {
                m_SBInfos.Add(new SBSEHats());
            }
        }
    }
}
