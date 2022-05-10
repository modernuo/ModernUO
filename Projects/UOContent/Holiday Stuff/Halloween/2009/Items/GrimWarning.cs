using ModernUO.Serialization;

namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */
    [SerializationGenerator(0, false)]
    public partial class GrimWarning : Item
    {
        [Constructible]
        public GrimWarning() : base(0x42BD)
        {
        }

        public override double DefaultWeight => 1;
    }
}
