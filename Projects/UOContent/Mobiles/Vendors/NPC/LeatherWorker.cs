using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class LeatherWorker : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public LeatherWorker() : base("the leather worker")
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBLeatherArmor());
            m_SBInfos.Add(new SBStuddedArmor());
            m_SBInfos.Add(new SBLeatherWorker());
        }
    }
}
