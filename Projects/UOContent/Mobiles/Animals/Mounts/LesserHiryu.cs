using System;
using System.Collections.Generic;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    public class LesserHiryu : BaseMount
    {
        private static readonly Dictionary<Mobile, ExpireTimer> m_Table = new();

        [Constructible]
        public LesserHiryu()
            : base("a lesser hiryu", 243, 0x3E94, AIType.AI_Melee, FightMode.Closest, 10, 1)
        {
            Hue = GetHue();

            SetStr(301, 410);
            SetDex(171, 270);
            SetInt(301, 325);

            SetHits(401, 600);
            SetMana(60);

            SetDamage(18, 23);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 70);
            SetResistance(ResistanceType.Fire, 60, 80);
            SetResistance(ResistanceType.Cold, 5, 15);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Anatomy, 75.1, 80.0);
            SetSkill(SkillName.MagicResist, 85.1, 100.0);
            SetSkill(SkillName.Tactics, 100.1, 110.0);
            SetSkill(SkillName.Wrestling, 100.1, 120.0);

            Fame = 10000;
            Karma = -10000;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 98.7;

            if (Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }
        }

        public LesserHiryu(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a hiryu corpse";
        public override double WeaponAbilityChance => 0.07; /* 1 in 15 chance of using; 1 in 5 chance of success */

        public override bool StatLossAfterTame => true;

        public override int TreasureMapLevel => 3;
        public override int Meat => 16;
        public override int Hides => 60;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override bool CanAngerOnTame => true;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;

        private static int GetHue()
        {
            var rand = Utility.Random(527);

            /*

            500 527 No Hue Color 94.88% 0
            10 527 Green   1.90% 0x8295
            10 527 Green   1.90% 0x8163 (Very Close to Above Green) //this one is an approximation
            5 527 Dark Green  0.95% 0x87D4
            1 527 Valorite  0.19% 0x88AB
            1 527 Midnight Blue 0.19% 0x8258

             * */

            if (rand <= 0)
            {
                return 0x8258;
            }

            if (rand <= 1)
            {
                return 0x88AB;
            }

            if (rand <= 6)
            {
                return 0x87D4;
            }

            if (rand <= 16)
            {
                return 0x8163;
            }

            if (rand <= 26)
            {
                return 0x8295;
            }

            return 0;
        }

        public override bool OverrideBondingReqs()
        {
            if (ControlMaster.Skills.Bushido.Base >= 90.0)
            {
                return true;
            }

            return false;
        }

        public override int GetAngerSound() => 0x4FE;

        public override int GetIdleSound() => 0x4FD;

        public override int GetAttackSound() => 0x4FC;

        public override int GetHurtSound() => 0x4FF;

        public override int GetDeathSound() => 0x4FB;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.Gems, 4);
        }

        public override double GetControlChance(Mobile m, bool useBaseSkill = false)
        {
            var tamingChance = base.GetControlChance(m, useBaseSkill);

            if (tamingChance >= 0.95)
            {
                return tamingChance;
            }

            var skill = useBaseSkill ? m.Skills.Bushido.Base : m.Skills.Bushido.Value;

            if (skill < 90.0)
            {
                return tamingChance;
            }

            var bushidoChance = (skill - 30.0) / 100;

            if (m.Skills.Bushido.Base >= 120)
            {
                bushidoChance += 0.05;
            }

            return bushidoChance > tamingChance ? bushidoChance : tamingChance;
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (Utility.RandomDouble() >= 0.1)
            {
                return;
            }

            /* Grasping Claw
               * Start cliloc: 1070836
               * Effect: Physical resistance -15% for 5 seconds
               * End cliloc: 1070838
               * Effect: Type: "3" - From: "0x57D4F5B" (player) - To: "0x0" - ItemId: "0x37B9" - ItemIdName: "glow" - FromLocation: "(1149 808, 32)" - ToLocation: "(1149 808, 32)" - Speed: "10" - Duration: "5" - FixedDirection: "True" - Explode: "False"
               */

            if (m_Table.TryGetValue(defender, out var timer))
            {
                timer.DoExpire();
                defender.SendLocalizedMessage(1070837); // The creature lands another blow in your weakened state.
            }
            else
            {
                defender.SendLocalizedMessage(
                    1070836
                ); // The blow from the creature's claws has made you more susceptible to physical attacks.
            }

            var effect = -(defender.PhysicalResistance * 15 / 100);

            var mod = new ResistanceMod(ResistanceType.Physical, effect);

            defender.FixedEffect(0x37B9, 10, 5);
            defender.AddResistanceMod(mod);

            timer = new ExpireTimer(defender, mod, TimeSpan.FromSeconds(5.0));
            timer.Start();
            m_Table[defender] = timer;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(2);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (version <= 1)
            {
                Timer.StartTimer(() => Fix(version));
            }

            if (version < 2)
            {
                for (var i = 0; i < Skills.Length; ++i)
                {
                    Skills[i].Cap = Math.Max(100.0, Skills[i].Cap * 0.9);

                    if (Skills[i].Base > Skills[i].Cap)
                    {
                        Skills[i].Base = Skills[i].Cap;
                    }
                }
            }
        }

        private void Fix(int version)
        {
            switch (version)
            {
                case 1:
                    {
                        if (InternalItem != null)
                        {
                            InternalItem.Hue = Hue;
                        }

                        goto case 0;
                    }
                case 0:
                    {
                        Hue = GetHue();
                        break;
                    }
            }
        }

        private class ExpireTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly ResistanceMod m_Mod;

            public ExpireTimer(Mobile m, ResistanceMod mod, TimeSpan delay)
                : base(delay)
            {
                m_Mobile = m;
                m_Mod = mod;
            }

            public void DoExpire()
            {
                m_Mobile.RemoveResistanceMod(m_Mod);
                Stop();
                m_Table.Remove(m_Mobile);
            }

            protected override void OnTick()
            {
                m_Mobile.SendLocalizedMessage(1070838); // Your resistance to physical attacks has returned.
                DoExpire();
            }
        }
    }
}
