using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Cobbler : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Cobbler() : base("the cobbler")
        {
            SetSkill(SkillName.Tailoring, 60.0, 83.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Sandals : VendorShoeType.Shoes;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBCobbler());
        }
    }
}
