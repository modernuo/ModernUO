using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator( 0 )]
public partial class SerpentsFangHighExecutioner : SerpentsFangAssassin
{
    public string CorpseName = "a black order high executioner corpse";

    [Constructible]
    public SerpentsFangHighExecutioner()
    {
        Name = "Black Order High Executioner";
        Title = "of the Serpent's Fang Sect";
        SetStr( 545, 560 );
        SetDex( 160, 175 );
        SetInt( 160, 175 );

        SetHits( 800 );
        SetStam( 190, 205 );

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

    public override void AlterMeleeDamageFrom( Mobile from, ref int damage )
    {
        if ( from != null )
        {
            from.Damage( damage / 2, from );
        }
    }

    public override void OnDeath( Container c )
    {
        base.OnDeath( c );

        c.DropItem( new SerpentFangKey() );

        if ( Utility.RandomDouble() < 0.5 )
        {
            c.DropItem( new SerpentFangSectBadge() );
        }
    }
}
