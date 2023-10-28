using ModernUO.Serialization;
using System;
using System.Runtime.CompilerServices;
using Server.Collections;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SavageShaman : BaseCreature
    {
        [Constructible]
        public SavageShaman() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("savage shaman");

            if (Utility.RandomBool())
            {
                Body = 184;
            }
            else
            {
                Body = 183;
            }

            SetStr(126, 145);
            SetDex(91, 110);
            SetInt(161, 185);

            SetDamage(4, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 77.5, 100.0);
            SetSkill(SkillName.Fencing, 62.5, 85.0);
            SetSkill(SkillName.Macing, 62.5, 85.0);
            SetSkill(SkillName.Magery, 72.5, 95.0);
            SetSkill(SkillName.Meditation, 77.5, 100.0);
            SetSkill(SkillName.MagicResist, 77.5, 100.0);
            SetSkill(SkillName.Swords, 62.5, 85.0);
            SetSkill(SkillName.Tactics, 62.5, 85.0);
            SetSkill(SkillName.Wrestling, 62.5, 85.0);

            Fame = 1000;
            Karma = -1000;

            PackReg(10, 15);
            PackItem(new Bandage(Utility.RandomMinMax(1, 15)));

            if (Utility.RandomDouble() < 0.1)
            {
                PackItem(new TribalBerry());
            }

            AddItem(new BoneArms());
            AddItem(new BoneLegs());
            AddItem(new DeerMask());
        }

        public override string CorpseName => "a savage corpse";

        public override int Meat => 1;
        public override bool AlwaysMurderer => true;
        public override bool ShowFameTitle => false;

        public override OppositionGroup OppositionGroup => OppositionGroup.SavagesAndOrcs;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }

        public override bool IsEnemy(Mobile m)
        {
            if (m.BodyMod == 183 || m.BodyMod == 184)
            {
                return false;
            }

            return base.IsEnemy(m);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            base.AggressiveAction(aggressor, criminal);

            if (aggressor.BodyMod == 183 || aggressor.BodyMod == 184)
            {
                AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0);
                aggressor.BodyMod = 0;
                aggressor.HueMod = -1;
                aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                aggressor.PlaySound(0x307);
                aggressor.SendLocalizedMessage(1040008); // Your skin is scorched as the tribal paint burns away!

                if (aggressor is PlayerMobile mobile)
                {
                    mobile.SavagePaintExpiration = TimeSpan.Zero;
                }
            }
        }

        public override void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
            if (to is Dragon or WhiteWyrm or SwampDragon or Drake or Nightmare or Hiryu or LesserHiryu or Daemon)
            {
                damage *= 3;
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);

            if (Utility.RandomDouble() < 0.1)
            {
                BeginSavageDance();
            }
        }

        public void BeginSavageDance()
        {
            if (Map == null)
            {
                return;
            }

            using var queue = PooledRefQueue<BaseCreature>.Create();

            foreach (var m in GetMobilesInRange(8))
            {
                if (m != this && m is SavageShaman ss)
                {
                    queue.Enqueue(ss);
                }
            }

            Animate(111, 5, 1, true, false, 0); // Do a little dance...

            if (AIObject != null)
            {
                AIObject.NextMove = Core.TickCount + 1000;
            }

            if (queue.Count < 3)
            {
                return;
            }

            while (queue.Count > 0)
            {
                var dancer = queue.Dequeue();

                dancer.Animate(111, 5, 1, true, false, 0); // Get down tonight...

                if (dancer.AIObject != null)
                {
                    dancer.AIObject.NextMove = Core.TickCount + 1000;
                }
            }

            Timer.StartTimer(TimeSpan.FromSeconds(1.0), EndSavageDance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanDoGreaterHeal(Mobile m, bool isFriendly) =>
            isFriendly && !m.Poisoned && !MortalStrike.IsWounded(m) && CanBeBeneficial(m);

        private void DoGreaterHeal(Mobile m)
        {
            DoBeneficial(m);

            // Algorithm: (40% of magery) + (1-10)

            var toHeal = (int)(Skills.Magery.Value * 0.4);
            toHeal += Utility.Random(1, 10);

            m.Heal(toHeal, this);

            m.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
            m.PlaySound(0x202);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanDoLightning(Mobile m, bool isFriendly) =>
            !isFriendly && CanBeHarmful(m) && (!m.Hidden || m.AccessLevel == AccessLevel.Player);

        private void DoLightning(Mobile m)
        {
            DoHarmful(m);

            double damage;

            if (Core.AOS)
            {
                var baseDamage = 6 + (int)(Skills.EvalInt.Value / 5.0);

                damage = Utility.RandomMinMax(baseDamage, baseDamage + 3);
            }
            else
            {
                damage = Utility.Random(12, 9);
            }

            m.BoltEffect(0);

            SpellHelper.Damage(TimeSpan.FromSeconds(0.25), m, this, damage, 0, 0, 0, 0, 100);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanDoPoison(Mobile m, bool isFriendly) =>
            !isFriendly && CanBeHarmful(m) && (!m.Hidden || m.AccessLevel == AccessLevel.Player);

        private void DoPoison(Mobile m)
        {
            DoHarmful(m);

            m.Spell?.OnCasterHurt();

            m.Paralyzed = false;

            var total = Skills.Magery.Value + Skills.Poisoning.Value;

            var dist = GetDistanceToSqrt(m);

            if (dist >= 3.0)
            {
                total -= (dist - 3.0) * 10.0;
            }

            int level = total switch
            {
                >= 200.0 => Utility.Random(10) == 0 ? 3 : 2,
                > 170.0  => 2,
                > 130.0  => 1,
                _        => 0
            };

            m.ApplyPoison(this, Poison.GetPoison(level));

            m.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
            m.PlaySound(0x474);
        }

        public void EndSavageDance()
        {
            if (Deleted)
            {
                return;
            }

            var rnd = Utility.Random(3);

            using var queue = PooledRefQueue<Mobile>.Create();
            foreach (var m in GetMobilesInRange(8))
            {
                var isFriendly = m is Savage or SavageRider or SavageShaman or SavageRidgeback;
                var shouldApply = rnd switch
                {
                    0 => CanDoGreaterHeal(m, isFriendly),
                    1 => CanDoLightning(m, isFriendly),
                    2 => CanDoPoison(m, isFriendly),
                };

                if (shouldApply)
                {
                    queue.Enqueue(m);
                }
            }

            while (queue.Count > 0)
            {
                var m = queue.Dequeue();

                switch (rnd)
                {
                    case 0:
                        {
                            DoGreaterHeal(m);
                            break;
                        }
                    case 1:
                        {
                            DoLightning(m);
                            break;
                        }
                    case 2:
                        {
                            DoPoison(m);
                            break;
                        }
                }
            }
        }
    }
}
