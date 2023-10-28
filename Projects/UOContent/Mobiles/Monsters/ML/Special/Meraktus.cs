using System;
using Server.Engines.CannedEvil;
using Server.Items;
using ModernUO.Serialization;
using Server.Collections;

namespace Server.Mobiles;

[SerializationGenerator(1, false)]
public partial class Meraktus : BaseChampion
{
    [SerializableField(0)]
    private Mobile[] _followers;

    [Constructible]
    public Meraktus() : base(AIType.AI_Melee)
    {
        Title = "the Tormented";
        Body = 263;
        BaseSoundID = 680;
        Hue = 0x835;

        SetStr(1419, 1438);
        SetDex(309, 413);
        SetInt(129, 131);

        SetHits(4100, 4200);

        SetDamage(16, 30);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 65, 90);
        SetResistance(ResistanceType.Fire, 65, 70);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 40, 60);
        SetResistance(ResistanceType.Energy, 50, 55);

        // SetSkill( SkillName.Meditation, Unknown );
        // SetSkill( SkillName.EvalInt, Unknown );
        // SetSkill( SkillName.Magery, Unknown );
        // SetSkill( SkillName.Poisoning, Unknown );
        SetSkill(SkillName.Anatomy, 0);
        SetSkill(SkillName.MagicResist, 107.0, 111.3);
        SetSkill(SkillName.Tactics, 107.0, 117.0);
        SetSkill(SkillName.Wrestling, 100.0, 105.0);

        Fame = 70000;
        Karma = -70000;

        VirtualArmor = 28; // Don't know what it should be

        if (Core.ML)
        {
            PackResources(8);
            PackTalismans(5);
        }
    }

    protected override void OnMapChange(Map oldMap)
    {
        if (_followers == null)
        {
            if (Map != null && Map != Map.Internal && (oldMap == null || oldMap == Map.Internal))
            {
                _followers = new Mobile[4];

                for (var i = 0; i < _followers.Length; i++)
                {
                    var follower = _followers[i] = new TormentedMinotaur();
                    follower.MoveToWorld(Location, Map);
                }
            }

            return;
        }

        for (var i = 0; i < _followers.Length; i++)
        {
            var follower = _followers[i];
            if (follower?.Deleted == false)
            {
                follower.MoveToWorld(Location, Map);
            }
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (_followers != null)
        {
            for (var i = 0; i < _followers.Length; i++)
            {
                _followers[i]?.Delete();
            }
        }
    }

    public override string CorpseName => "the remains of Meraktus";
    public override ChampionSkullType SkullType => ChampionSkullType.Pain;

    public override Type[] UniqueList => new[] { typeof(Subdue) };
    public override Type[] SharedList => Array.Empty<Type>();

    public override Type[] DecorativeList => new[]
    {
        typeof(ArtifactLargeVase),
        typeof(ArtifactVase),
        typeof(MinotaurStatueDeed)
    };

    public override MonsterStatuetteType[] StatueTypes => new[] { MonsterStatuetteType.Minotaur };

    public override string DefaultName => "Meraktus";

    public override int Meat => 2;
    public override int Hides => 10;
    public override HideType HideType => HideType.Regular;
    public override Poison PoisonImmune => Poison.Regular;
    public override int TreasureMapLevel => 3;
    public override bool BardImmune => true;
    public override bool Unprovokable => true;
    public override bool Uncalmable => true;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;

    public virtual void PackResources(int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            PackItem(
                Utility.Random(6) switch
                {
                    0 => new Blight(),
                    1 => new Scourge(),
                    2 => new Taint(),
                    3 => new Putrefication(),
                    4 => new Corruption(),
                    _ => new Muculent() // 5
                }
            );
        }
    }

    public virtual void PackTalismans(int amount)
    {
        var count = Utility.Random(amount);

        for (var i = 0; i < count; i++)
        {
            PackItem(new RandomTalisman());
        }
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (!Core.ML)
        {
            return;
        }

        c.DropItem(new MalletAndChisel());

        c.DropItem(
            Utility.Random(3) switch
            {
                0 => new MinotaurHedge(),
                1 => new LightYarn(),
                _ => new BonePile() // 2
            }
        );

        if (Utility.RandomBool())
        {
            c.DropItem(new TormentedChains());
        }

        if (Utility.RandomDouble() < 0.025)
        {
            c.DropItem(new CrimsonCincture());
        }
    }

    public override void GenerateLoot()
    {
        if (Core.ML)
        {
            AddLoot(LootPack.AosSuperBoss, 5); // Need to verify
        }
    }

    public override int GetAngerSound() => 0x597;

    public override int GetIdleSound() => 0x596;

    public override int GetAttackSound() => 0x599;

    public override int GetHurtSound() => 0x59a;

    public override int GetDeathSound() => 0x59c;

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
        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (var m in GetMobilesInRange(8))
        {
            if (m.Deleted || m == this || !CanBeHarmful(m))
            {
                continue;
            }

            if (m.Player || m is BaseCreature bc && (bc.Controlled || bc.Summoned || bc.Team != Team))
            {
                queue.Enqueue(m);
            }
        }

        while (queue.Count > 0)
        {
            var m = queue.Dequeue();

            if (m is PlayerMobile pm && pm.Mounted)
            {
                pm.Mount.Rider = null;
            }

            var damage = Math.Clamp((int)(m.Hits * 0.6), 10, 75);

            DoHarmful(m);
            AOS.Damage(m, this, damage, 100, 0, 0, 0, 0);
            if (m.Alive && m.Body.IsHuman && !m.Mounted)
            {
                m.Animate(20, 7, 1, true, false, 0); // take hit
            }
        }
    }

    private void MigrateFrom(V0Content content)
    {
    }
}
