using System;
using ModernUO.Serialization;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class LordOaks : BaseChampion
{
    [SerializableField(0)]
    private BaseCreature _queen;

    [SerializableField(1)]
    private bool _spawnedQueen;

    [Constructible]
    public LordOaks() : base(AIType.AI_Mage, FightMode.Evil)
    {
        Body = 175;
        SetStr(403, 850);
        SetDex(101, 150);
        SetInt(503, 800);

        SetHits(3000);
        SetStam(202, 400);

        SetDamage(21, 33);

        SetDamageType(ResistanceType.Physical, 75);
        SetDamageType(ResistanceType.Fire, 25);

        SetResistance(ResistanceType.Physical, 85, 90);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 80, 90);
        SetResistance(ResistanceType.Energy, 80, 90);

        SetSkill(SkillName.Anatomy, 75.1, 100.0);
        SetSkill(SkillName.EvalInt, 120.1, 130.0);
        SetSkill(SkillName.Magery, 120.0);
        SetSkill(SkillName.Meditation, 120.1, 130.0);
        SetSkill(SkillName.MagicResist, 100.5, 150.0);
        SetSkill(SkillName.Tactics, 100.0);
        SetSkill(SkillName.Wrestling, 100.0);

        Fame = 22500;
        Karma = 22500;

        VirtualArmor = 100;
    }

    public override ChampionSkullType SkullType => ChampionSkullType.Enlightenment;

    public override Type[] UniqueList => new[] { typeof(OrcChieftainHelm) };

    public override Type[] SharedList => new[]
    {
        typeof(RoyalGuardSurvivalKnife),
        typeof(DjinnisRing),
        typeof(LieutenantOfTheBritannianRoyalGuard),
        typeof(SamaritanRobe),
        typeof(DetectiveBoots),
        typeof(TheMostKnowledgePerson)
    };

    public override Type[] DecorativeList => new[]
    {
        typeof(WaterTile),
        typeof(WindSpirit),
        typeof(Pier),
        typeof(DirtPatch)
    };

    public override MonsterStatuetteType[] StatueTypes => Array.Empty<MonsterStatuetteType>();

    public override string DefaultName => "Lord Oaks";

    public override bool AutoDispel => true;
    public override bool CanFly => true;
    public override bool BardImmune => !Core.SE;
    public override bool Unprovokable => Core.SE;
    public override bool Uncalmable => Core.SE;
    public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

    public override Poison PoisonImmune => Poison.Deadly;

    private static MonsterAbility[] _abilities = { MonsterAbilities.SummonPixiesCounter };
    public override MonsterAbility[] GetMonsterAbilities() => CheckQueen() ? _abilities : MonsterAbilities.Empty;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 5);
    }

    public override int GetAngerSound() => 0x2F8;

    public override int GetIdleSound() => 0x2F8;

    public override int GetAttackSound() => Utility.Random(0x2F5, 2);

    public override int GetHurtSound() => 0x2F9;

    public override int GetDeathSound() => 0x2F7;

    public bool CheckQueen()
    {
        if (Map == null)
        {
            return false;
        }

        if (!_spawnedQueen)
        {
            Say(1042153); // Come forth my queen!

            _queen = new Silvani { Team = Team };
            _queen.MoveToWorld(Location, Map);

            _spawnedQueen = true;
            return true;
        }

        if (_queen?.Deleted != false)
        {
            _queen = null;
            return false;
        }

        return true;
    }

    public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
    {
        if (CheckQueen())
        {
            scalar *= 0.1;
        }
    }

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);

        defender.Damage(Utility.Random(20, 10), this);
        defender.Stam -= Utility.Random(20, 10);
        defender.Mana -= Utility.Random(20, 10);
    }

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        attacker.Damage(Utility.Random(20, 10), this);
        attacker.Stam -= Utility.Random(20, 10);
        attacker.Mana -= Utility.Random(20, 10);
    }
}
