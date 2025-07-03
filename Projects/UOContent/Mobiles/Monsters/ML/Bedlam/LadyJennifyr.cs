using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class LadyJennifyr : SkeletalKnight
{
    [Constructible]
    public LadyJennifyr()
    {
        IsParagon = true;

        Hue = 0x76D;

        SetStr(208, 309);
        SetDex(91, 118);
        SetInt(44, 101);

        SetHits(1113, 1285);

        SetDamage(15, 25);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Cold, 60);

        SetResistance(ResistanceType.Physical, 56, 65);
        SetResistance(ResistanceType.Fire, 41, 49);
        SetResistance(ResistanceType.Cold, 71, 80);
        SetResistance(ResistanceType.Poison, 41, 50);
        SetResistance(ResistanceType.Energy, 50, 58);

        SetSkill(SkillName.Wrestling, 127.9, 137.1);
        SetSkill(SkillName.Tactics, 128.4, 141.9);
        SetSkill(SkillName.MagicResist, 102.1, 119.5);
        SetSkill(SkillName.Anatomy, 129.0, 137.5);

        Fame = 18000;
        Karma = -18000;
    }

    public override string CorpseName => "a Lady Jennifyr corpse";
    public override string DefaultName => "Lady Jennifyr";

    /*
    // TODO: Uncomment once added
    public override void OnDeath( Container c )
    {
      base.OnDeath( c );

      if (Utility.RandomDouble() < 0.15)
        c.DropItem( new DisintegratingThesisNotes() );

      if (Utility.RandomDouble() < 0.1)
        c.DropItem( new ParrotItem() );
    }
    */

    public override bool GivesMLMinorArtifact => true;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 3);
    }

    private static readonly MonsterAbility[] _abilities =
    [
        new FanningFire(0.10, -10, 35, 45)
    ];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;
}
