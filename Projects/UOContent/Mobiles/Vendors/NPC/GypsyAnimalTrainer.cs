using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class GypsyAnimalTrainer : AnimalTrainer
    {
        [Constructible]
        public GypsyAnimalTrainer()
        {
            if (Utility.RandomBool())
            {
                Title = "the gypsy animal trainer";
            }
            else
            {
                Title = "the gypsy animal herder";
            }
        }

        public override VendorShoeType ShoeType => Female ? VendorShoeType.ThighBoots : VendorShoeType.Boots;

        public override int GetShoeHue() => 0;

        public override void InitOutfit()
        {
            base.InitOutfit();

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
