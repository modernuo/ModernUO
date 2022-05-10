using System;
using System.Collections.Generic;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles
{
    public class SavageShaman : BaseCreature
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

        public SavageShaman(Serial serial) : base(serial)
        {
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

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

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

            var list = new List<SavageShaman>();

            foreach (var m in GetMobilesInRange(8))
            {
                if (m != this && m is SavageShaman ss)
                {
                    list.Add(ss);
                }
            }

            Animate(111, 5, 1, true, false, 0); // Do a little dance...

            if (AIObject != null)
            {
                AIObject.NextMove = Core.TickCount + 1000;
            }

            if (list.Count >= 3)
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    var dancer = list[i];

                    dancer.Animate(111, 5, 1, true, false, 0); // Get down tonight...

                    if (dancer.AIObject != null)
                    {
                        dancer.AIObject.NextMove = Core.TickCount + 1000;
                    }
                }

                Timer.StartTimer(TimeSpan.FromSeconds(1.0), EndSavageDance);
            }
        }

        public void EndSavageDance()
        {
            if (Deleted)
            {
                return;
            }

            var eable = GetMobilesInRange(8);

            switch (Utility.Random(3))
            {
                case 0: /* greater heal */
                    {
                        foreach (var m in eable)
                        {
                            var isFriendly = m is Savage or SavageRider or SavageShaman or SavageRidgeback;

                            if (!isFriendly)
                            {
                                continue;
                            }

                            if (m.Poisoned || MortalStrike.IsWounded(m) || !CanBeBeneficial(m))
                            {
                                continue;
                            }

                            DoBeneficial(m);

                            // Algorithm: (40% of magery) + (1-10)

                            var toHeal = (int)(Skills.Magery.Value * 0.4);
                            toHeal += Utility.Random(1, 10);

                            m.Heal(toHeal, this);

                            m.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
                            m.PlaySound(0x202);
                        }

                        break;
                    }
                case 1: /* lightning */
                    {
                        foreach (var m in eable)
                        {
                            var isFriendly = m is Savage or SavageRider or SavageShaman or SavageRidgeback;

                            if (isFriendly)
                            {
                                continue;
                            }

                            if (!CanBeHarmful(m))
                            {
                                continue;
                            }

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

                        break;
                    }
                case 2: /* poison */
                    {
                        foreach (var m in eable)
                        {
                            var isFriendly = m is Savage or SavageRider or SavageShaman or SavageRidgeback;

                            if (isFriendly)
                            {
                                continue;
                            }

                            if (!CanBeHarmful(m))
                            {
                                continue;
                            }

                            DoHarmful(m);

                            m.Spell?.OnCasterHurt();

                            m.Paralyzed = false;

                            var total = Skills.Magery.Value + Skills.Poisoning.Value;

                            var dist = GetDistanceToSqrt(m);

                            if (dist >= 3.0)
                            {
                                total -= (dist - 3.0) * 10.0;
                            }

                            int level;

                            if (total >= 200.0 && Utility.Random(1, 100) <= 10)
                            {
                                level = 3;
                            }
                            else if (total > 170.0)
                            {
                                level = 2;
                            }
                            else if (total > 130.0)
                            {
                                level = 1;
                            }
                            else
                            {
                                level = 0;
                            }

                            m.ApplyPoison(this, Poison.GetPoison(level));

                            m.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
                            m.PlaySound(0x474);
                        }

                        break;
                    }
            }

            eable.Free();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
