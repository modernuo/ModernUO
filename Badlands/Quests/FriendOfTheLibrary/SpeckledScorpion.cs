using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator( 0 )]
public partial class SpeckledScorpion : Scorpion
{
    [Constructible]
    public SpeckledScorpion()
    {
        Name = "a speckled scorpion";
        Tamable = false;
    }

    public override string CorpseName => "a speckled scorpion corpse";

    public override void OnDeath( Container c )
    {
        base.OnDeath( c );

        if ( Utility.RandomDouble() < 0.4 )
        {
            c.DropItem( new SpeckledPoisonSac() );
        }
    }
}
