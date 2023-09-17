using System;
using ModernUO.Serialization;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(1, false)]
public partial class Neira : BaseChampion
{
    private const double SpeedBoostScalar = 1.2;

    private bool _speedBoost;

    [Constructible]
    public Neira() : base(AIType.AI_Mage)
    {
        Title = "the necromancer";
        Body = 401;
        Hue = 0x83EC;

        SetStr(305, 425);
        SetDex(72, 150);
        SetInt(505, 750);

        SetHits(4800);
        SetStam(102, 300);

        SetDamage(25, 35);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 25, 30);
        SetResistance(ResistanceType.Fire, 35, 45);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 20, 30);

        SetSkill(SkillName.EvalInt, 120.0);
        SetSkill(SkillName.Magery, 120.0);
        SetSkill(SkillName.Meditation, 120.0);
        SetSkill(SkillName.MagicResist, 150.0);
        SetSkill(SkillName.Tactics, 97.6, 100.0);
        SetSkill(SkillName.Wrestling, 97.6, 100.0);

        Fame = 22500;
        Karma = -22500;

        VirtualArmor = 30;
        Female = true;

        Item shroud = new HoodedShroudOfShadows();
        shroud.Movable = false;

        AddItem(shroud);

        var weapon = new Scimitar
        {
            Skill = SkillName.Wrestling,
            Hue = 38,
            Movable = false
        };

        AddItem(weapon);

        // new SkeletalMount().Rider = this;
        AddItem(new VirtualMountItem(this));
    }

    public override ChampionSkullType SkullType => ChampionSkullType.Death;

    public override Type[] UniqueList => new[] { typeof(ShroudOfDeciet) };

    public override Type[] SharedList => new[]
    {
        typeof(ANecromancerShroud),

        typeof(CaptainJohnsHat)
    };

    public override Type[] DecorativeList => new[] { typeof(WallBlood), typeof(TatteredAncientMummyWrapping) };

    public override MonsterStatuetteType[] StatueTypes => Array.Empty<MonsterStatuetteType>();

    public override string DefaultName => "Neira";

    public override bool AlwaysMurderer => true;
    public override bool BardImmune => !Core.SE;
    public override bool Unprovokable => Core.SE;
    public override bool Uncalmable => Core.SE;
    public override Poison PoisonImmune => Poison.Deadly;

    public override bool ShowFameTitle => false;
    public override bool ClickTitle => false;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 3);
        AddLoot(LootPack.Meager);
    }

    public override bool OnBeforeDeath()
    {
        var mount = Mount;

        if (mount != null)
        {
            mount.Rider = null;
        }

        (mount as Item)?.Delete();

        return base.OnBeforeDeath();
    }

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        CheckSpeedBoost();
        base.OnDamage(amount, from, willKill);
    }

    private void CheckSpeedBoost()
    {
        if (Hits < HitsMax / 4)
        {
            if (!_speedBoost)
            {
                ActiveSpeed /= SpeedBoostScalar;
                PassiveSpeed /= SpeedBoostScalar;
                _speedBoost = true;
            }
        }
        else if (_speedBoost)
        {
            ActiveSpeed *= SpeedBoostScalar;
            PassiveSpeed *= SpeedBoostScalar;
            _speedBoost = false;
        }
    }

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);

        if (Utility.RandomDouble() < 0.1) // 10% chance to drop or throw an unholy bone
        {
            AddUnholyBone(defender, 0.25);
        }

        CheckSpeedBoost();
    }

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        if (Utility.RandomDouble() < 0.1) // 10% chance to drop or throw an unholy bone
        {
            AddUnholyBone(attacker, 0.25);
        }
    }

    public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
    {
        base.AlterDamageScalarFrom(caster, ref scalar);

        if (Utility.RandomDouble() < 0.1) // 10% chance to throw an unholy bone
        {
            AddUnholyBone(caster, 1.0);
        }
    }

    public void AddUnholyBone(Mobile target, double chanceToThrow)
    {
        if (Map == null)
        {
            return;
        }

        if (chanceToThrow >= Utility.RandomDouble())
        {
            Direction = GetDirectionTo(target);
            MovingEffect(target, 0xF7E, 10, 1, true, false, 0x496, 0);
            new DelayTimer(this, target).Start();
        }
        else
        {
            new UnholyBone().MoveToWorld(Location, Map);
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        // We don't need to serialize this
        reader.ReadBool(); // _speedBoost
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        CheckSpeedBoost();
    }

    private class DelayTimer : Timer
    {
        private readonly Mobile m_Mobile;
        private readonly Mobile m_Target;

        public DelayTimer(Mobile m, Mobile target) : base(TimeSpan.FromSeconds(1.0))
        {
            m_Mobile = m;
            m_Target = target;
        }

        protected override void OnTick()
        {
            if (m_Mobile.CanBeHarmful(m_Target))
            {
                m_Mobile.DoHarmful(m_Target);
                AOS.Damage(m_Target, m_Mobile, Utility.RandomMinMax(10, 20), 100, 0, 0, 0, 0);
                new UnholyBone().MoveToWorld(m_Target.Location, m_Target.Map);
            }
        }
    }
}
