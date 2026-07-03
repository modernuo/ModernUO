using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class StoneSlith : BaseCreature
{
    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.GraspingClaw, MonsterAbilities.TailSwipe];

    [Constructible]
    public StoneSlith() : base(AIType.AI_Melee)
    {
        Body = 734;

        SetStr(250, 300);
        SetDex(76, 90);
        SetInt(34, 69);

        SetHits(154, 166);
        SetStam(76, 90);
        SetMana(34, 69);

        SetDamage(6, 24);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 55);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.MagicResist, 86.8, 95.1);
        SetSkill(SkillName.Tactics, 82.6, 88.6);
        SetSkill(SkillName.Wrestling, 75.8, 87.4);
        SetSkill(SkillName.Anatomy, 0.0, 2.9);

        Tamable = true;
        MinTameSkill = 65.1;
        ControlSlots = 2;

        // TODO(SA-creatures): unported vs ServUO — DragonBlood carving, and the minor drops
        // SlithEye / TatteredAncientScroll / AncientPotteryFragments (item classes not yet in ModernUO).
    }

    public override string CorpseName => "a slith corpse";
    public override string DefaultName => "a stone slith";

    public override int Meat => 1;
    public override int Hides => 12;
    public override HideType HideType => HideType.Spined;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average, 2);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (!Controlled && Utility.RandomDouble() <= 0.005)
        {
            c.DropItem(new StoneSlithClaw());
        }
    }
}
