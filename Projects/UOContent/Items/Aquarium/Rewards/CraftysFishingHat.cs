using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class CraftysFishingHat : BaseHat
    {
        [Constructible]
        public CraftysFishingHat() : base(0x1713)
        {
        }

        public override int LabelNumber => 1074572; // Crafty's Fishing Hat

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }
    }
}
