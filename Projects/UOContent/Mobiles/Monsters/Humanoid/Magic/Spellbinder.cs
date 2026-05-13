using ModernUO.Serialization;
using Server.Spells;


namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class Spellbinder : BaseCreature
{
    [Constructible]
    public Spellbinder() : base(AIType.AI_Mage, FightMode.Aggressor)
    {
        Body = Utility.RandomList(26, 50, 56);
        BaseSoundID = 0x482;

        SetStr(46, 70);
        SetDex(47, 65);
        SetInt(187, 210);

        SetHits(36, 50);

        SetDamage(3, 6);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 50);

        SetResistance(ResistanceType.Physical, 25, 30);
        SetResistance(ResistanceType.Cold, 15, 25);
        SetResistance(ResistanceType.Poison, 10, 20);

        SetSkill(SkillName.MagicResist, 35.1, 50.0);
        SetSkill(SkillName.Tactics, 35.1, 50.0);
        SetSkill(SkillName.Wrestling, 35.1, 45.0);

        Fame = 2500;
        Karma = -2500;

        VirtualArmor = 28;
    }

    protected override BaseAI ForcedAI => new SpellbinderAI(this);

    public override string CorpseName => "a ghostly corpse";
    public override string DefaultName => "a spectral spellbinder";

    public override bool BleedImmune => true;

    public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

    public override Poison PoisonImmune => Poison.Regular;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
        PackItem(Loot.RandomWeapon());
    }

    public class SpellbinderAI : MageAI{
        public SpellbinderAI(BaseCreature m) : base(m)
        {
        }

        public override Spell GetRandomDamageSpell() => GetRandomCurseSpell();
    }
}
