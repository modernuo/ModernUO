using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class InnKeeper : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public InnKeeper() : base("the innkeeper")
        {
        }
        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Sandals : VendorShoeType.Shoes;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBInnKeeper());

            if (IsTokunoVendor)
            {
                m_SBInfos.Add(new SBSEFood());
            }
        }
    }
}
