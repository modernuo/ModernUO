using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator( 0 )]
public partial class FireRabbit : VorpalBunny
{
    [Constructible]
    public FireRabbit()
    {
        Name = "a fire rabbit";

        Hue = 0x550; // guessed

        SetStr( 130 );
        SetDex( 4500 );
        SetInt( 2500 );

        SetHits( 2500 );
        SetStam( 1500 );
        SetMana( 1500 );

        SetDamage( 10, 15 );

        SetDamageType( ResistanceType.Fire, 100 );
        SetDamageType( ResistanceType.Physical, 0 );

        SetResistance( ResistanceType.Physical, 45 );
        SetResistance( ResistanceType.Fire, 100 );
        SetResistance( ResistanceType.Cold, 40 );
        SetResistance( ResistanceType.Poison, 46 );
        SetResistance( ResistanceType.Energy, 46 );

        SetSkill( SkillName.MagicResist, 200 );
        SetSkill( SkillName.Tactics, 0.0 );
        SetSkill( SkillName.Wrestling, 80.0 );
        SetSkill( SkillName.Anatomy, 0.0 );
    }

    public override string CorpseName => "a hare corpse";
    public override bool IsScaryToPets => true;
    public override bool BardImmune => true;

    public override void OnDeath( Container c )
    {
        base.OnDeath( c );

        if ( Utility.RandomDouble() < 0.5 )
        {
            c.DropItem( new AnimalPheromone() );
        }
    }

    public override void GenerateLoot()
    {
        AddLoot( LootPack.FilthyRich );
        AddLoot( LootPack.Rich, 3 );
    }
}
