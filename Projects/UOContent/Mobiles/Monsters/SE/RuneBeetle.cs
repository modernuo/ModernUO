using System;
using System.Collections.Generic;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    public class RuneBeetle : BaseCreature
    {
        private static readonly Dictionary<Mobile, ExpireTimer> m_Table = new();

        [Constructible]
        public RuneBeetle() : base(AIType.AI_Mage)
        {
            Body = 244;

            SetStr(401, 460);
            SetDex(121, 170);
            SetInt(376, 450);

            SetHits(301, 360);

            SetDamage(15, 22);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Poison, 10);
            SetDamageType(ResistanceType.Energy, 70);

            SetResistance(ResistanceType.Physical, 40, 65);
            SetResistance(ResistanceType.Fire, 35, 50);
            SetResistance(ResistanceType.Cold, 35, 50);
            SetResistance(ResistanceType.Poison, 75, 95);
            SetResistance(ResistanceType.Energy, 40, 60);

            SetSkill(SkillName.EvalInt, 100.1, 125.0);
            SetSkill(SkillName.Magery, 100.1, 110.0);
            SetSkill(SkillName.Poisoning, 120.1, 140.0);
            SetSkill(SkillName.MagicResist, 95.1, 110.0);
            SetSkill(SkillName.Tactics, 78.1, 93.0);
            SetSkill(SkillName.Wrestling, 70.1, 77.5);

            Fame = 15000;
            Karma = -15000;

            if (Utility.RandomDouble() < .25)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }

            PackItem(
                Utility.Random(10) switch
                {
                    0 => new LeftArm(),
                    1 => new RightArm(),
                    2 => new Torso(),
                    3 => new Bone(),
                    4 => new RibCage(),
                    5 => new RibCage(),
                    _ => new BonePile() // 6-9
                }
            );

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 93.9;
        }

        public RuneBeetle(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a rune beetle corpse";
        public override string DefaultName => "a rune beetle";

        public override Poison PoisonImmune => Poison.Greater;
        public override Poison HitPoison => Poison.Greater;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;
        public override bool CanAngerOnTame => true;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

        public override int GetAngerSound() => 0x4E8;

        public override int GetIdleSound() => 0x4E7;

        public override int GetAttackSound() => 0x4E6;

        public override int GetHurtSound() => 0x4E9;

        public override int GetDeathSound() => 0x4E5;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.MedScrolls, 1);
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (Utility.RandomDouble() >= 0.05)
            {
                return;
            }

            /* Rune Corruption
             * Start cliloc: 1070846 "The creature magically corrupts your armor!"
             * Effect: All resistances -70 (lowest 0) for 5 seconds
             * End ASCII: "The corruption of your armor has worn off"
             */

            if (m_Table.TryGetValue(defender, out var timer))
            {
                timer.DoExpire();
                defender.SendLocalizedMessage(1070845); // The creature continues to corrupt your armor!
            }
            else
            {
                defender.SendLocalizedMessage(1070846); // The creature magically corrupts your armor!
            }

            var mods = new List<ResistanceMod>();

            if (Core.ML)
            {
                if (defender.PhysicalResistance > 0)
                {
                    mods.Add(new ResistanceMod(ResistanceType.Physical, -(defender.PhysicalResistance / 2)));
                }

                if (defender.FireResistance > 0)
                {
                    mods.Add(new ResistanceMod(ResistanceType.Fire, -(defender.FireResistance / 2)));
                }

                if (defender.ColdResistance > 0)
                {
                    mods.Add(new ResistanceMod(ResistanceType.Cold, -(defender.ColdResistance / 2)));
                }

                if (defender.PoisonResistance > 0)
                {
                    mods.Add(new ResistanceMod(ResistanceType.Poison, -(defender.PoisonResistance / 2)));
                }

                if (defender.EnergyResistance > 0)
                {
                    mods.Add(new ResistanceMod(ResistanceType.Energy, -(defender.EnergyResistance / 2)));
                }
            }
            else
            {
                if (defender.PhysicalResistance > 0)
                {
                    mods.Add(
                        new ResistanceMod(
                            ResistanceType.Physical,
                            defender.PhysicalResistance > 70 ? -70 : -defender.PhysicalResistance
                        )
                    );
                }

                if (defender.FireResistance > 0)
                {
                    mods.Add(
                        new ResistanceMod(
                            ResistanceType.Fire,
                            defender.FireResistance > 70 ? -70 : -defender.FireResistance
                        )
                    );
                }

                if (defender.ColdResistance > 0)
                {
                    mods.Add(
                        new ResistanceMod(
                            ResistanceType.Cold,
                            defender.ColdResistance > 70 ? -70 : -defender.ColdResistance
                        )
                    );
                }

                if (defender.PoisonResistance > 0)
                {
                    mods.Add(
                        new ResistanceMod(
                            ResistanceType.Poison,
                            defender.PoisonResistance > 70 ? -70 : -defender.PoisonResistance
                        )
                    );
                }

                if (defender.EnergyResistance > 0)
                {
                    mods.Add(
                        new ResistanceMod(
                            ResistanceType.Energy,
                            defender.EnergyResistance > 70 ? -70 : -defender.EnergyResistance
                        )
                    );
                }
            }

            for (var i = 0; i < mods.Count; ++i)
            {
                defender.AddResistanceMod(mods[i]);
            }

            defender.FixedEffect(0x37B9, 10, 5);

            timer = new ExpireTimer(defender, mods, TimeSpan.FromSeconds(5.0));
            timer.Start();
            m_Table[defender] = timer;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (version < 1)
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

        private class ExpireTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly List<ResistanceMod> m_Mods;

            public ExpireTimer(Mobile m, List<ResistanceMod> mods, TimeSpan delay) : base(delay)
            {
                m_Mobile = m;
                m_Mods = mods;
            }

            public void DoExpire()
            {
                for (var i = 0; i < m_Mods.Count; ++i)
                {
                    m_Mobile.RemoveResistanceMod(m_Mods[i]);
                }

                Stop();
                m_Table.Remove(m_Mobile);
            }

            protected override void OnTick()
            {
                m_Mobile.SendMessage("The corruption of your armor has worn off");
                DoExpire();
            }
        }
    }
}
