using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Vagabond : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Vagabond() : base("the vagabond")
        {
            SetSkill(SkillName.ItemID, 60.0, 83.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBTinker());
            m_SBInfos.Add(new SBVagabond());
        }

        public override void InitOutfit()
        {
            AddItem(new FancyShirt(Utility.RandomBrightHue()));
            AddItem(new Shoes(GetShoeHue()));
            AddItem(new LongPants(GetRandomHue()));

            if (Utility.RandomBool())
            {
                AddItem(new Cloak(Utility.RandomBrightHue()));
            }

            AddItem(
                Utility.RandomBool()
                    ? new SkullCap(Utility.RandomNeutralHue())
                    : new Bandana(Utility.RandomNeutralHue())
            );

            Utility.AssignRandomHair(this);
            Utility.AssignRandomFacialHair(this, HairHue);

            PackGold(100, 200);
        }
    }
}
