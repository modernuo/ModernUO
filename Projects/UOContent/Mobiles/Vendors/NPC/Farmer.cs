using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Farmer : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Farmer() : base("the farmer")
        {
            SetSkill(SkillName.Lumberjacking, 36.0, 68.0);
            SetSkill(SkillName.TasteID, 36.0, 68.0);
            SetSkill(SkillName.Cooking, 36.0, 68.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => VendorShoeType.ThighBoots;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBFarmer());
        }

        public override int GetShoeHue() => 0;

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new WideBrimHat(Utility.RandomNeutralHue()));
        }
    }
}
