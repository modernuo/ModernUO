using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Armorer : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Armorer() : base("the armorer")
        {
            SetSkill(SkillName.ArmsLore, 64.0, 100.0);
            SetSkill(SkillName.Blacksmith, 60.0, 83.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => VendorShoeType.Boots;

        public override void InitSBInfo()
        {
            switch (Utility.Random(4))
            {
                case 0:
                    {
                        m_SBInfos.Add(new SBLeatherArmor());
                        m_SBInfos.Add(new SBStuddedArmor());
                        m_SBInfos.Add(new SBMetalShields());
                        m_SBInfos.Add(new SBPlateArmor());
                        m_SBInfos.Add(new SBHelmetArmor());
                        m_SBInfos.Add(new SBChainmailArmor());
                        m_SBInfos.Add(new SBRingmailArmor());
                        break;
                    }
                case 1:
                    {
                        m_SBInfos.Add(new SBStuddedArmor());
                        m_SBInfos.Add(new SBLeatherArmor());
                        m_SBInfos.Add(new SBMetalShields());
                        m_SBInfos.Add(new SBHelmetArmor());
                        break;
                    }
                case 2:
                    {
                        m_SBInfos.Add(new SBMetalShields());
                        m_SBInfos.Add(new SBPlateArmor());
                        m_SBInfos.Add(new SBHelmetArmor());
                        m_SBInfos.Add(new SBChainmailArmor());
                        m_SBInfos.Add(new SBRingmailArmor());
                        break;
                    }
                case 3:
                    {
                        m_SBInfos.Add(new SBMetalShields());
                        m_SBInfos.Add(new SBHelmetArmor());
                        break;
                    }
            }

            if (IsTokunoVendor)
            {
                m_SBInfos.Add(new SBSELeatherArmor());
                m_SBInfos.Add(new SBSEArmor());
            }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new HalfApron(Utility.RandomYellowHue()));
            AddItem(new Bascinet());
        }
    }
}
