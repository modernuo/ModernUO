using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class VarietyDealer : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public VarietyDealer() : base("the variety dealer")
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBVarietyDealer());
        }
    }
}
