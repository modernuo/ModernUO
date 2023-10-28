using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Engines.CannedEvil;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Serado : BaseChampion
{
    [Constructible]
    public Serado() : base(AIType.AI_Melee)
    {
        Title = "the awakened";

        Body = 249;
        Hue = 0x96C;

        SetStr(1000);
        SetDex(150);
        SetInt(300);

        SetHits(9000);
        SetMana(300);

        SetDamage(29, 35);

        SetDamageType(ResistanceType.Physical, 70);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 10);

        SetResistance(ResistanceType.Physical, 30);
        SetResistance(ResistanceType.Fire, 60);
        SetResistance(ResistanceType.Cold, 60);
        SetResistance(ResistanceType.Poison, 90);
        SetResistance(ResistanceType.Energy, 50);

        SetSkill(SkillName.MagicResist, 120.0);
        SetSkill(SkillName.Tactics, 120.0);
        SetSkill(SkillName.Wrestling, 70.0);
        SetSkill(SkillName.Poisoning, 150.0);

        Fame = 22500;
        Karma = -22500;

        PackItem(Seed.RandomBonsaiSeed());
    }

    public override ChampionSkullType SkullType => ChampionSkullType.Power;

    public override Type[] UniqueList => new[] { typeof(Pacify) };

    public override Type[] SharedList => new[]
    {
        typeof(BraveKnightOfTheBritannia),
        typeof(DetectiveBoots),
        typeof(EmbroideredOakLeafCloak),
        typeof(LieutenantOfTheBritannianRoyalGuard)
    };

    public override Type[] DecorativeList => new[] { typeof(Futon), typeof(SwampTile) };

    public override MonsterStatuetteType[] StatueTypes => Array.Empty<MonsterStatuetteType>();

    public override string DefaultName => "Serado";

    public override int TreasureMapLevel => 5;

    public override Poison HitPoison => Poison.Lethal;
    public override Poison PoisonImmune => Poison.Lethal;
    public override double HitPoisonChance => 0.8;

    public override int Feathers => 30;

    public override bool ShowFameTitle => false;
    public override bool ClickTitle => false;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.DoubleStrike;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 4);
        AddLoot(LootPack.FilthyRich);
        AddLoot(LootPack.Gems, 6);
    }

    // TODO: Hit Lightning Area

    public override void OnDamagedBySpell(Mobile attacker, int damage)
    {
        base.OnDamagedBySpell(attacker, damage);

        ScaleResistances();
        DoCounter(attacker);
    }

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        ScaleResistances();
        DoCounter(attacker);
    }

    private void ScaleResistances()
    {
        var hitsLost = (HitsMax - Hits) / (double)HitsMax;

        SetResistance(ResistanceType.Physical, 30 + (int)(hitsLost * (95 - 30)));
        SetResistance(ResistanceType.Fire, 60 + (int)(hitsLost * (95 - 60)));
        SetResistance(ResistanceType.Cold, 60 + (int)(hitsLost * (95 - 60)));
        SetResistance(ResistanceType.Poison, 90 + (int)(hitsLost * (95 - 90)));
        SetResistance(ResistanceType.Energy, 50 + (int)(hitsLost * (95 - 50)));
    }

    private void DoCounter(Mobile attacker)
    {
        if (Map == null)
        {
            return;
        }

        if (!(Utility.RandomDouble() < 0.2))
        {
            return;
        }

        Mobile target = null;

        if (attacker is BaseCreature bcAttacker)
        {
            if (bcAttacker.BardProvoked)
            {
                return;
            }

            target = bcAttacker.GetMaster();
        }

        /* Counterattack with Hit Poison Area
         * 20-25 damage, unresistable
         * Lethal poison, 100% of the time
         * Particle effect: Type: "2" From: "0x4061A107" To: "0x0" ItemId: "0x36BD" ItemIdName: "explosion" FromLocation: "(296 615, 17)" ToLocation: "(296 615, 17)" Speed: "1" Duration: "10" FixedDirection: "True" Explode: "False" Hue: "0xA6" RenderMode: "0x0" Effect: "0x1F78" ExplodeEffect: "0x1" ExplodeSound: "0x0" Serial: "0x4061A107" Layer: "255" Unknown: "0x0"
         * Doesn't work on provoked monsters
         */

        if (target?.InRange(this, 25) != true)
        {
            target = attacker;
        }

        Animate(10, 4, 1, true, false, 0);

        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (var m in target.GetMobilesInRange<Mobile>(8))
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
            DoHarmful(m);

            AOS.Damage(m, this, Utility.RandomMinMax(20, 25), true, 0, 0, 0, 100, 0);

            m.FixedParticles(0x36BD, 1, 10, 0x1F78, 0xA6, 0, (EffectLayer)255);
            m.ApplyPoison(this, Poison.Lethal);
        }
    }
}
