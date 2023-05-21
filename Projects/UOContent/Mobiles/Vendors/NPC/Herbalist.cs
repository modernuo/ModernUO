using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Herbalist : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Herbalist() : base("the herbalist")
        {
            SetSkill(SkillName.Alchemy, 80.0, 100.0);
            SetSkill(SkillName.Cooking, 80.0, 100.0);
            SetSkill(SkillName.TasteID, 80.0, 100.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.MagesGuild;

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBHerbalist());
        }
    }
}
