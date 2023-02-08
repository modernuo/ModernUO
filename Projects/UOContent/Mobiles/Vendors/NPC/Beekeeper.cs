using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Beekeeper : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Beekeeper() : base("the beekeeper")
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => VendorShoeType.Boots;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBBeekeeper());
        }
    }
}
