using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Thief : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Thief() : base("the thief")
        {
            SetSkill(SkillName.Camping, 55.0, 78.0);
            SetSkill(SkillName.DetectHidden, 65.0, 88.0);
            SetSkill(SkillName.Hiding, 45.0, 68.0);
            SetSkill(SkillName.Archery, 65.0, 88.0);
            SetSkill(SkillName.Tracking, 65.0, 88.0);
            SetSkill(SkillName.Veterinary, 60.0, 83.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBThief());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Shirt(Utility.RandomNeutralHue()));
            AddItem(new LongPants(Utility.RandomNeutralHue()));
            AddItem(new Dagger());
            AddItem(new ThighBoots(Utility.RandomNeutralHue()));
        }
    }
}
