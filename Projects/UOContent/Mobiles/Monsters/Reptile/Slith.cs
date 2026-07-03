using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class Slith : BaseCreature
{
    // ServUO SpecialAbility.DragonBreath -> closest ModernUO monster ability.
    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    [Constructible]
    public Slith() : base(AIType.AI_Melee)
    {
        Body = 734;

        SetStr(129, 136);
        SetDex(72, 75);
        SetInt(12, 13);

        SetHits(84, 85);

        SetDamage(6, 24);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 35, 40);
        SetResistance(ResistanceType.Fire, 35, 45);
        SetResistance(ResistanceType.Poison, 25, 35);
        SetResistance(ResistanceType.Energy, 25, 30);

        SetSkill(SkillName.MagicResist, 59.1, 63.5);
        SetSkill(SkillName.Tactics, 74.6, 76.4);
        SetSkill(SkillName.Wrestling, 62.0, 77.1);

        Tamable = true;
        ControlSlots = 1;
        MinTameSkill = 80.7;

        // TODO(SA-creatures): unported vs ServUO — DragonBlood carving and the minor drops
        // SlithEye / AncientPotteryFragments / TatteredAncientScroll (item classes not yet in ModernUO).
    }

    public override string CorpseName => "a slith corpse";
    public override string DefaultName => "a slith";

    public override int TreasureMapLevel => 2;
    public override int Meat => 6;
    public override int Hides => 10;

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average, 2);
    }
}
