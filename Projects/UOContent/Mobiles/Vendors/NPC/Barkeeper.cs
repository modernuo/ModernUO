using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Barkeeper : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Barkeeper() : base("the barkeeper")
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.ThighBoots : VendorShoeType.Boots;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBBarkeeper());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new HalfApron(Utility.RandomBrightHue()));
        }
    }
}
