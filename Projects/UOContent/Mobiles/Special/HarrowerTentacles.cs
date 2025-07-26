using System;
using ModernUO.Serialization;
using Server.Collections;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class HarrowerTentacles : BaseCreature
{
    private DrainTimer _timer;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Harrower _harrower;

    [Constructible]
    public HarrowerTentacles(Harrower harrower = null) : base(AIType.AI_Melee)
    {
        _harrower = harrower;
        Body = 129;

        SetStr(901, 1000);
        SetDex(126, 140);
        SetInt(1001, 1200);

        SetHits(541, 600);

        SetDamage(13, 20);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 35, 45);
        SetResistance(ResistanceType.Cold, 35, 45);
        SetResistance(ResistanceType.Poison, 35, 45);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetSkill(SkillName.Meditation, 100.0);
        SetSkill(SkillName.MagicResist, 120.1, 140.0);
        SetSkill(SkillName.Swords, 90.1, 100.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 90.1, 100.0);

        Fame = 15000;
        Karma = -15000;

        VirtualArmor = 60;

        _timer = new DrainTimer(this);
        _timer.Start();

        PackReg(50);
        PackNecroReg(15, 75);
    }

    public override string CorpseName => "a tentacles corpse";

    public override string DefaultName => "tentacles of the harrower";

    public override bool AutoDispel => true;
    public override bool Unprovokable => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override bool DisallowAllMoves => true;

    public override void CheckReflect(Mobile caster, ref bool reflect)
    {
        reflect = true;
    }

    public override int GetIdleSound() => 0x101;

    public override int GetAngerSound() => 0x5E;

    public override int GetDeathSound() => 0x1C2;

    public override int GetAttackSound() => -1;

    public override int GetHurtSound() => 0x289;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
        AddLoot(LootPack.MedScrolls, 3);
        AddLoot(LootPack.HighScrolls, 2);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _timer = new DrainTimer(this);
        _timer.Start();
    }

    public override void OnAfterDelete()
    {
        _timer?.Stop();
        _timer = null;

        base.OnAfterDelete();
        Harrower?.RemoveFromTentacles(this);
    }

    private class DrainTimer : Timer
    {
        private readonly HarrowerTentacles m_Owner;

        public DrainTimer(HarrowerTentacles owner) : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
        {
            m_Owner = owner;
        }

        protected override void OnTick()
        {
            if (m_Owner.Deleted)
            {
                Stop();
                return;
            }

            using var queue = PooledRefQueue<Mobile>.Create();
            foreach (var m in m_Owner.GetMobilesInRange<Mobile>(9))
            {
                if (m == m_Owner || !m_Owner.CanBeHarmful(m))
                {
                    continue;
                }

                if (m.Player && !m.Alive)
                {
                    continue;
                }

                if (m is BaseCreature bc && !(bc.Controlled || bc.IsAnimatedDead || bc.Summoned || bc.Team != m_Owner.Team))
                {
                    continue;
                }

                queue.Enqueue(m);
            }

            while (queue.Count > 0)
            {
                var m = queue.Dequeue();
                m_Owner.DoHarmful(m);

                m.FixedParticles(0x374A, 10, 15, 5013, 0x455, 0, EffectLayer.Waist);
                m.PlaySound(0x1F1);

                var drain = Utility.RandomMinMax(14, 30);

                m_Owner.Hits += drain;

                if (m_Owner.Harrower != null)
                {
                    m_Owner.Harrower.Hits += drain;
                }

                m.Damage(drain, m_Owner);
            }
        }
    }
}
