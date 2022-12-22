using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.GargoyleAlchemist")]

    [SerializationGenerator(0, false)]
    public partial class Glassblower : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Glassblower() : base("the alchemist")
        {
            SetSkill(SkillName.Alchemy, 85.0, 100.0);
            SetSkill(SkillName.TasteID, 85.0, 100.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.MagesGuild;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBGlassblower());
            m_SBInfos.Add(new SBAlchemist());
        }
    }
}
