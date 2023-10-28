using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Rikktor : BaseChampion
{
    [Constructible]
    public Rikktor() : base(AIType.AI_Melee)
    {
        Body = 172;

        SetStr(701, 900);
        SetDex(201, 350);
        SetInt(51, 100);

        SetHits(3000);
        SetStam(203, 650);

        SetDamage(28, 55);

        SetDamageType(ResistanceType.Physical, 25);
        SetDamageType(ResistanceType.Fire, 50);
        SetDamageType(ResistanceType.Energy, 25);

        SetResistance(ResistanceType.Physical, 80, 90);
        SetResistance(ResistanceType.Fire, 80, 90);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 80, 90);
        SetResistance(ResistanceType.Energy, 80, 90);

        SetSkill(SkillName.Anatomy, 100.0);
        SetSkill(SkillName.MagicResist, 140.2, 160.0);
        SetSkill(SkillName.Tactics, 100.0);

        Fame = 22500;
        Karma = -22500;

        VirtualArmor = 130;
    }

    public override ChampionSkullType SkullType => ChampionSkullType.Power;

    public override Type[] UniqueList => new[] { typeof(CrownOfTalKeesh) };

    public override Type[] SharedList => new[]
    {
        typeof(TheMostKnowledgePerson),
        typeof(BraveKnightOfTheBritannia),
        typeof(LieutenantOfTheBritannianRoyalGuard)
    };

    public override Type[] DecorativeList => new[]
    {
        typeof(LavaTile),
        typeof(MonsterStatuette),
        typeof(MonsterStatuette)
    };

    public override MonsterStatuetteType[] StatueTypes => new[]
    {
        MonsterStatuetteType.OphidianArchMage,
        MonsterStatuetteType.OphidianWarrior
    };

    public override string DefaultName => "Rikktor";

    public override Poison PoisonImmune => Poison.Lethal;
    public override ScaleType ScaleType => ScaleType.All;
    public override int Scales => 20;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 4);
    }

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);

        if (Utility.RandomDouble() < 0.2)
        {
            Earthquake();
        }
    }

    public void Earthquake()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        PlaySound(0x2F3);

        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (var m in GetMobilesInRange<Mobile>(8))
        {
            if (m == this || !(CanBeHarmful(m) || m.Player && m.Alive))
            {
                continue;
            }

            if (m is not BaseCreature bc || !(bc.Controlled || bc.Summoned || bc.Team != Team))
            {
                continue;
            }

            queue.Enqueue(m);
        }

        while (queue.Count > 0)
        {
            var m = queue.Dequeue();

            var damage = Math.Clamp((int)(m.Hits * 0.6), 10, 75);

            DoHarmful(m);

            AOS.Damage(m, this, damage, 100, 0, 0, 0, 0);

            if (m.Alive && m.Body.IsHuman && !m.Mounted)
            {
                m.Animate(20, 7, 1, true, false, 0); // take hit
            }
        }
    }

    public override int GetAngerSound() => Utility.Random(0x2CE, 2);

    public override int GetIdleSound() => 0x2D2;

    public override int GetAttackSound() => Utility.Random(0x2C7, 5);

    public override int GetHurtSound() => 0x2D1;

    public override int GetDeathSound() => 0x2CC;
}
