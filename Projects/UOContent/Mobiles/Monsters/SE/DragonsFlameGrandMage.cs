using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator( 0 )]
public partial class DragonsFlameGrandMage : DragonsFlameMage
{
    [Constructible]
    public DragonsFlameGrandMage()
    {
        Name = "Black Order Grand Mage";
        Title = "of the Dragon's Flame Sect";
        SetStr( 340, 360 );
        SetDex( 200, 215 );
        SetInt( 500, 515 );

        SetHits( 800 );

        SetDamage( 15, 20 );

        Fame = 25000;
        Karma = -25000;
    }

    public override string CorpseName => "a black order grand mage corpse";


    public override bool AlwaysMurderer => true;
    public override bool ShowFameTitle => false;

    public override void GenerateLoot()
    {
        AddLoot( LootPack.FilthyRich, 6 );
    }

    public override void OnDeath( Container c )
    {
        base.OnDeath( c );

        c.DropItem( new DragonFlameKey() );

        if ( Utility.RandomDouble() < 0.5 )
        {
            c.DropItem( new DragonFlameSectBadge() );
        }
    }
}
