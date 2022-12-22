using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class GypsyMaiden : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public GypsyMaiden() : base("the gypsy maiden")
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override bool GetGender() => true;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBProvisioner());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(
                Utility.Random(4) switch
                {
                    0 => new JesterHat(Utility.RandomBrightHue()),
                    1 => new Bandana(Utility.RandomBrightHue()),
                    2 => new SkullCap(Utility.RandomBrightHue()),
                    _ => null // 3
                }
            );

            if (Utility.RandomBool())
            {
                AddItem(new HalfApron(Utility.RandomBrightHue()));
            }

            var item = FindItemOnLayer(Layer.Pants);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.OuterLegs);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.InnerLegs);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }
        }
    }
}
