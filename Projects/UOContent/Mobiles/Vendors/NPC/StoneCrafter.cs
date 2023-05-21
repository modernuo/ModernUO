using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.GargoyleStonecrafter")]
    [SerializationGenerator(0, false)]
    public partial class StoneCrafter : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public StoneCrafter() : base("the stone crafter")
        {
            SetSkill(SkillName.Carpentry, 85.0, 100.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.TinkersGuild;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBStoneCrafter());
            m_SBInfos.Add(new SBStavesWeapon());
            m_SBInfos.Add(new SBCarpenter());
            m_SBInfos.Add(new SBWoodenShields());
        }
    }
}
