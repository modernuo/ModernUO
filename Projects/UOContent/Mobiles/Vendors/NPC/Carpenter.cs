using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Carpenter : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Carpenter() : base("the carpenter")
        {
            SetSkill(SkillName.Carpentry, 85.0, 100.0);
            SetSkill(SkillName.Lumberjacking, 60.0, 83.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.TinkersGuild;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBStavesWeapon());
            m_SBInfos.Add(new SBCarpenter());
            m_SBInfos.Add(new SBWoodenShields());

            if (IsTokunoVendor)
            {
                m_SBInfos.Add(new SBSECarpenter());
            }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new HalfApron());
        }
    }
}
