using ModernUO.Serialization;

namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */
    [SerializationGenerator(0, false)]
    public partial class MurkyMilk : Pitcher
    {
        [Constructible]
        public MurkyMilk() : base(BeverageType.Milk)
        {
            Hue = 0x3e5;
            Quantity = MaxQuantity;
            ItemID = Utility.RandomBool() ? 0x09F0 : 0x09AD;
        }

        public override string DefaultName => "Murky Milk";
        public override int MaxQuantity => 5;
        public override double DefaultWeight => 1;
    }
}
