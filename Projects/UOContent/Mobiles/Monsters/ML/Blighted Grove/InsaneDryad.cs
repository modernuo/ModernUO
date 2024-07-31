using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator( 0, false )]
public partial class InsaneDryad : MLDryad
{
    [Constructible]
    public InsaneDryad()
    {
        Fame = 7000;
        Karma = -7000;
    }


    public override string CorpseName => "an insane dryad corpse";
    public override bool InitialInnocent => false;

    public override string DefaultName => "an insane dryad";

    public override void OnDeath( Container c )
    {
        base.OnDeath( c );

        if ( Utility.RandomDouble() < 0.1 )
        {
            c.DropItem( new ParrotItem() );
        }
    }
}
