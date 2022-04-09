using ModernUO.Serialization;

namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */
    [SerializationGenerator(0, false)]
    public partial class SkullsOnPike : Item
    {
        [Constructible]
        public SkullsOnPike() : base(0x42B5)
        {
        }

        public override double DefaultWeight => 1;
    }
}
