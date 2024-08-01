using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class EcruCitrineRing : GoldRing
{
    [Constructible]
    public EcruCitrineRing()
    {
        Weight = 1.0;

        if ( .75 > Utility.RandomDouble() )
        {
            Attributes.EnhancePotions = 50;
        }
        else
        {
            Attributes.BonusStr = Utility.RandomMinMax( 5, 6 );
        }
    }

    public override int LabelNumber => 1073457; // ecru citrine ring
}
