using ModernUO.Serialization;

namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */
    [SerializationGenerator(0, false)]
    public partial class PumpkinPizza : CheesePizza
    {
        [Constructible]
        public PumpkinPizza() => Hue = 0xF3;

        public override string DefaultName => "Pumpkin Pizza";
    }
}
