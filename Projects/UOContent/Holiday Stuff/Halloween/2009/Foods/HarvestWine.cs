using ModernUO.Serialization;

namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */
    [SerializationGenerator(0, false)]
    public partial class HarvestWine : BeverageBottle
    {
        [Constructible]
        public HarvestWine() : base(BeverageType.Wine) => Hue = 0xe0;

        public override string DefaultName => "Harvest Wine";
        public override double DefaultWeight => 1;
    }
}
