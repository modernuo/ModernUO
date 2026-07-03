using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class ToxicSlith : BaseCreature
{
    // ServUO SpecialAbility.DragonBreath -> closest ModernUO monster ability.
    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    [Constructible]
    public ToxicSlith() : base(AIType.AI_Melee)
    {
        Body = 734;
        Hue = 476;

        SetStr(223, 306);
        SetDex(231, 258);
        SetInt(30, 35);

        SetHits(197, 215);
        SetStam(231, 258);

        SetDamage(6, 24);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 0, 9);
        SetResistance(ResistanceType.Cold, 5, 10);
        SetResistance(ResistanceType.Poison, 100, 100);
        SetResistance(ResistanceType.Energy, 5, 7);

        SetSkill(SkillName.MagicResist, 95.4, 98.3);
        SetSkill(SkillName.Tactics, 85.5, 90.9);
        SetSkill(SkillName.Wrestling, 90.4, 95.1);
        SetSkill(SkillName.Poisoning, 90.0, 110.0);

        // TODO(SA-creatures): unported vs ServUO — DragonBlood carving and the minor drops
        // ToxicVenomSac / SlithEye / AncientPotteryFragments / TatteredAncientScroll (not yet in ModernUO).
    }

    public override string CorpseName => "a slith corpse";
    public override string DefaultName => "a toxic slith";

    public override int Meat => 6;
    public override int Hides => 11;
    public override HideType HideType => HideType.Horned;

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average, 2);
    }
}
