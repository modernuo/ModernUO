using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Miner : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Miner() : base("the miner")
        {
            SetSkill(SkillName.Mining, 65.0, 88.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBMiner());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new FancyShirt(0x3E4));
            AddItem(new LongPants(0x192));
            AddItem(new Pickaxe());
            AddItem(new ThighBoots(0x283));
        }
    }
}
