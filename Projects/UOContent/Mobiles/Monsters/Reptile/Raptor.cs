using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class Raptor : BaseCreature
{
    [Constructible]
    public Raptor() : base(AIType.AI_Melee)
    {
        Body = 730;

        SetStr(404, 471);
        SetDex(132, 155);
        SetInt(105, 145);

        SetHits(343, 400);

        SetDamage(11, 17);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 45, 50);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.MagicResist, 75.1, 90.0);
        SetSkill(SkillName.Tactics, 75.1, 100.0);
        SetSkill(SkillName.Wrestling, 70.1, 95.1);

        Fame = 7500;
        Karma = -7500;

        Tamable = true;
        MinTameSkill = 107.1;
        ControlSlots = 2;

        // TODO(SA-creatures): ServUO Raptors summon up to 2 "friend" raptors on entering combat
        // (InternalTimer/CheckFriends). Omitted pending a ModernUO timer/MonsterAbility adaptation.
        // Also unported vs ServUO: the 25% AncientPotteryFragments corpse drop (item class not yet in ModernUO).
    }

    public override string CorpseName => "a raptor corpse";
    public override string DefaultName => "a raptor";

    public override int TreasureMapLevel => 3;
    public override int Meat => 7;
    public override int Hides => 11;
    public override HideType HideType => HideType.Horned;
    public override PackInstinct PackInstinct => PackInstinct.Ostard;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

    public override int GetIdleSound() => 1573;
    public override int GetAngerSound() => 1570;
    public override int GetHurtSound() => 1572;
    public override int GetDeathSound() => 1571;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (!Controlled && Utility.RandomDouble() <= 0.005)
        {
            c.DropItem(new RaptorClaw());
        }
    }
}
