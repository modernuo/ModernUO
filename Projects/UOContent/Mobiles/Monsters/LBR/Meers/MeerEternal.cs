using ModernUO.Serialization;
using System;
using Server.Collections;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MeerEternal : BaseCreature
    {
        private DateTime m_NextAbilityTime;

        [Constructible]
        public MeerEternal() : base(AIType.AI_Mage, FightMode.Evil)
        {
            Body = 772;

            SetStr(416, 505);
            SetDex(146, 165);
            SetInt(566, 655);

            SetHits(250, 303);

            SetDamage(11, 13);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 45, 55);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 150.5, 200.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 18000;
            Karma = 18000;

            VirtualArmor = 34;

            m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(2, 5));
        }

        public override string CorpseName => "a meer's corpse";
        public override string DefaultName => "a meer eternal";

        public override bool AutoDispel => true;
        public override bool BardImmune => !Core.AOS;
        public override bool CanRummageCorpses => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override int TreasureMapLevel => Core.AOS ? 5 : 4;

        public override bool InitialInnocent => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.MedScrolls, 2);
            AddLoot(LootPack.HighScrolls, 2);
        }

        public override int GetHurtSound() => 0x167;

        public override int GetDeathSound() => 0xBC;

        public override int GetAttackSound() => 0x28B;

        private void DoAreaLeech()
        {
            m_NextAbilityTime += TimeSpan.FromSeconds(2.5);

            Say(true, "Beware, mortals!  You have provoked my wrath!");
            FixedParticles(0x376A, 10, 10, 9537, 33, 0, EffectLayer.Waist);

            Timer.StartTimer(TimeSpan.FromSeconds(5.0), DoAreaLeech_Finish);
        }

        private void DoAreaLeech_Finish()
        {
            var eable = GetMobilesInRange(6);
            using var queue = PooledRefQueue<Mobile>.Create();
            foreach (var m in eable)
            {
                if (CanBeHarmful(m) && IsEnemy(m))
                {
                    queue.Enqueue(m);
                }
            }
            eable.Free();

            if (queue.Count == 0)
            {
                Say(true, "Bah! You have escaped my grasp this time, mortal!");
                return;
            }

            double scalar = queue.Count switch
            {
                1 => 0.75,
                2 => 0.50,
                _ => 0.25
            };

            while (queue.Count > 0)
            {
                var m = queue.Dequeue();

                var damage = (int)(m.Hits * scalar) + Utility.RandomMinMax(-5, 5);

                m.MovingParticles(this, 0x36F4, 1, 0, false, false, 32, 0, 9535, 1, 0, (EffectLayer)255, 0x100);
                m.MovingParticles(this, 0x0001, 1, 0, false, true, 32, 0, 9535, 9536, 0, (EffectLayer)255, 0);

                DoHarmful(m);
                Hits += AOS.Damage(m, this, Math.Max(damage, 1), 100, 0, 0, 0, 0);
            }

            Say(true, "If I cannot cleanse thy soul, I will destroy it!");
        }

        private void DoFocusedLeech(Mobile combatant, string message)
        {
            Say(true, message);

            Timer.StartTimer(TimeSpan.FromSeconds(0.5), () => DoFocusedLeech_Stage1(combatant));
        }

        private void DoFocusedLeech_Stage1(Mobile combatant)
        {
            if (CanBeHarmful(combatant))
            {
                MovingParticles(combatant, 0x36FA, 1, 0, false, false, 1108, 0, 9533, 1, 0, (EffectLayer)255, 0x100);
                MovingParticles(combatant, 0x0001, 1, 0, false, true, 1108, 0, 9533, 9534, 0, (EffectLayer)255, 0);
                PlaySound(0x1FB);

                Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => DoFocusedLeech_Stage2(combatant));
            }
        }

        private void DoFocusedLeech_Stage2(Mobile combatant)
        {
            if (CanBeHarmful(combatant))
            {
                combatant.MovingParticles(this, 0x36F4, 1, 0, false, false, 32, 0, 9535, 1, 0, (EffectLayer)255, 0x100);
                combatant.MovingParticles(this, 0x0001, 1, 0, false, true, 32, 0, 9535, 9536, 0, (EffectLayer)255, 0);

                PlaySound(0x209);
                DoHarmful(combatant);
                Hits += AOS.Damage(combatant, this, Utility.RandomMinMax(30, 40) - (Core.AOS ? 0 : 10), 100, 0, 0, 0, 0);
            }
        }

        public override void OnThink()
        {
            if (Core.Now >= m_NextAbilityTime)
            {
                var combatant = Combatant;

                if (combatant != null && combatant.Map == Map && combatant.InRange(this, 12))
                {
                    m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(10, 15));

                    var ability = Utility.Random(4);

                    switch (ability)
                    {
                        case 0:
                            {
                                DoFocusedLeech(combatant, "Thine essence will fill my withering body with strength!");
                                break;
                            }
                        case 1:
                            {
                                DoFocusedLeech(
                                    combatant,
                                    "I rebuke thee, worm, and cleanse thy vile spirit of its tainted blood!"
                                );
                                break;
                            }
                        case 2:
                            {
                                DoFocusedLeech(combatant, "I devour your life's essence to strengthen my resolve!");
                                break;
                            }
                        case 3:
                            {
                                DoAreaLeech();
                                break;
                            }
                        // TODO: Resurrect ability
                    }
                }
            }

            base.OnThink();
        }
    }
}
