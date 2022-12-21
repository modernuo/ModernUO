using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Architect : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Architect() : base("the architect")
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.TinkersGuild;

        public override void InitSBInfo()
        {
            if (!Core.AOS)
            {
                m_SBInfos.Add(new SBHouseDeed());
            }

            m_SBInfos.Add(new SBArchitect());
        }
    }
}
