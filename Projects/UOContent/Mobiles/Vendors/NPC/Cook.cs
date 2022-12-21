using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Cook : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Cook() : base("the cook")
        {
            SetSkill(SkillName.Cooking, 90.0, 100.0);
            SetSkill(SkillName.TasteID, 75.0, 98.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Sandals : VendorShoeType.Shoes;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBCook());

            if (IsTokunoVendor)
            {
                m_SBInfos.Add(new SBSECook());
            }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new HalfApron());
        }
    }
}
