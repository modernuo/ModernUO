using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class GypsyBanker : Banker
    {
        [Constructible]
        public GypsyBanker() => Title = "the gypsy banker";

        public override bool IsActiveVendor => false;
        public override NpcGuild NpcGuild => NpcGuild.None;
        public override bool ClickTitle => false;

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

            var item = FindItemOnLayer(Layer.Pants);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.Shoes);

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

            item = FindItemOnLayer(Layer.OuterTorso);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.InnerTorso);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }

            item = FindItemOnLayer(Layer.Shirt);

            if (item != null)
            {
                item.Hue = Utility.RandomBrightHue();
            }
        }
    }
}
