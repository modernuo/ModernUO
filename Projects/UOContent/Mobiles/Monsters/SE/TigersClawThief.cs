using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator( 0 )]
public partial class TigersClawThief : BaseCreature
{
    [Constructible]
    public TigersClawThief() : base( AIType.AI_Melee )
    {
        Name = "Black Order Thief";
        Title = "of the Tiger's Claw Sect";
        Female = Utility.RandomBool();
        Race = Race.Human;
        Hue = Race.RandomSkinHue();
        HairItemID = Race.RandomHair( Female );
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair( this );


        SetWearable( new ThighBoots(), 0x51D, 1 );
        SetWearable( new FancyShirt(), 0x51D, 1 );
        SetWearable( new StuddedMempo(), dropChance: 1 );
        SetWearable( new JinBaori(), 0x69, 1 );
        SetWearable( new StuddedGloves(), 0x69, 1 );
        SetWearable( new LeatherNinjaPants(), 0x51D, 1 );
        SetWearable( new LightPlateJingasa(), 0x51D, 1 );
        SetWearable( new Wakizashi(), dropChance: 1 );

        SetStr( 340, 360 );
        SetDex( 400, 415 );
        SetInt( 200, 215 );

        SetHits( 800, 815 );

        SetDamage( 13, 15 );

        SetDamageType( ResistanceType.Physical, 100 );

        SetResistance( ResistanceType.Physical, 45, 65 );
        SetResistance( ResistanceType.Fire, 60, 70 );
        SetResistance( ResistanceType.Cold, 55, 60 );
        SetResistance( ResistanceType.Poison, 30, 50 );
        SetResistance( ResistanceType.Energy, 30, 50 );

        SetSkill( SkillName.MagicResist, 80.0, 100.0 );
        SetSkill( SkillName.Tactics, 115.0, 130.0 );
        SetSkill( SkillName.Wrestling, 95.0, 120.0 );
        SetSkill( SkillName.Anatomy, 105.0, 120.0 );
        SetSkill( SkillName.Fencing, 78.0, 100.0 );
        SetSkill( SkillName.Swords, 90.1, 105.0 );
        SetSkill( SkillName.Ninjitsu, 90.0, 120.0 );
        SetSkill( SkillName.Hiding, 100.0, 120.0 );
        SetSkill( SkillName.Stealth, 100.0, 120.0 );

        Fame = 13000;
        Karma = -13000;
    }

    public override string CorpseName => "a black order thief corpse";

    public override bool AlwaysMurderer => true;
    public override bool ShowFameTitle => false;

    public override void GenerateLoot()
    {
        AddLoot( LootPack.FilthyRich, 4 );
    }

    public override void OnDeath( Container c )
    {
        base.OnDeath( c );

        if ( Utility.RandomDouble() < 0.3 )
        {
            c.DropItem( new TigerClawSectBadge() );
        }
    }
}
