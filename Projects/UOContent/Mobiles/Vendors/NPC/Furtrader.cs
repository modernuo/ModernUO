using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Furtrader : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Furtrader() : base("the furtrader")
        {
            SetSkill(SkillName.Camping, 55.0, 78.0);
            // SetSkill( SkillName.Alchemy, 60.0, 83.0 );
            SetSkill(SkillName.AnimalLore, 85.0, 100.0);
            SetSkill(SkillName.Cooking, 45.0, 68.0);
            SetSkill(SkillName.Tracking, 36.0, 68.0);
        }
        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBFurtrader());
        }
    }
}
