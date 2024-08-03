using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator( 0 )]
public partial class TigersClawMaster : TigersClawThief
{
    public string CorpseName = "a black order master corpse";

    [Constructible]
    public TigersClawMaster()
    {
        Name = "Black Order Master";
        Title = "of the Serpent's Fang Sect";
        SetStr( 440, 460 );
        SetDex( 400, 415 );
        SetInt( 200, 215 );

        SetHits( 850, 875 );

        SetDamage( 15, 20 );

        Fame = 25000;
        Karma = -25000;
    }

    public override bool AlwaysMurderer => true;
    public override bool ShowFameTitle => false;

    public override void GenerateLoot()
    {
        AddLoot( LootPack.FilthyRich, 6 );
    }

    public override void OnDeath( Container c )
    {
        base.OnDeath( c );

        c.DropItem( new TigerClawKey() );

        if ( Utility.RandomDouble() < 0.5 )
        {
            c.DropItem( new TigerClawSectBadge() );
        }
    }
}
