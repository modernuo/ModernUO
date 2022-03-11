using System;
using System.Collections.Generic;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    public class TsukiWolf : BaseCreature
    {
        private static readonly Dictionary<Mobile, ExpireTimer> m_Table = new();

        [Constructible]
        public TsukiWolf()
            : base(AIType.AI_Melee)
        {
            Body = 250;
            Hue = Utility.Random(3) == 0 ? Utility.RandomNeutralHue() : 0;

            SetStr(401, 450);
            SetDex(151, 200);
            SetInt(66, 76);

            SetHits(376, 450);
            SetMana(40);

            SetDamage(14, 18);

            SetDamageType(ResistanceType.Physical, 90);
            SetDamageType(ResistanceType.Cold, 5);
            SetDamageType(ResistanceType.Energy, 5);

            SetResistance(ResistanceType.Physical, 40, 60);
            SetResistance(ResistanceType.Fire, 50, 70);
            SetResistance(ResistanceType.Cold, 50, 70);
            SetResistance(ResistanceType.Poison, 50, 70);
            SetResistance(ResistanceType.Energy, 50, 70);

            SetSkill(SkillName.Anatomy, 65.1, 72.0);
            SetSkill(SkillName.MagicResist, 65.1, 70.0);
            SetSkill(SkillName.Tactics, 95.1, 110.0);
            SetSkill(SkillName.Wrestling, 97.6, 107.5);

            Fame = 8500;
            Karma = -8500;

            if (Core.ML && Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomPeculiarSeed(1));
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
        }

        public TsukiWolf(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a tsuki wolf corpse";
        public override string DefaultName => "a tsuki wolf";
        public override int Meat => 4;
        public override int Hides => 25;
        public override FoodType FavoriteFood => FoodType.Meat;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Rich);
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (Utility.RandomDouble() >= 0.1)
            {
                return;
            }

            /* Blood Bath
               * Start cliloc 1070826
               * Sound: 0x52B
               * 2-3 blood spots
               * Damage: 2 hps per second for 5 seconds
               * End cliloc: 1070824
               */

            if (m_Table.TryGetValue(defender, out var timer))
            {
                timer.DoExpire();
                defender.SendLocalizedMessage(1070825); // The creature continues to rage!
            }
            else
            {
                defender.SendLocalizedMessage(1070826); // The creature goes into a rage, inflicting heavy damage!
            }

            timer = new ExpireTimer(defender, this);
            timer.Start();
            m_Table[defender] = timer;
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

        public override int GetAngerSound() => 0x52D;

        public override int GetIdleSound() => 0x52C;

        public override int GetAttackSound() => 0x52B;

        public override int GetHurtSound() => 0x52E;

        public override int GetDeathSound() => 0x52A;

        private class ExpireTimer : Timer
        {
            private readonly Mobile m_From;
            private readonly Mobile m_Mobile;
            private int m_Count;

            public ExpireTimer(Mobile m, Mobile from)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Mobile = m;
                m_From = from;
            }

            public void DoExpire()
            {
                Stop();
                m_Table.Remove(m_Mobile);
            }

            public void DrainLife()
            {
                if (m_Mobile.Alive)
                {
                    m_Mobile.Damage(2, m_From);
                }
                else
                {
                    DoExpire();
                }
            }

            protected override void OnTick()
            {
                DrainLife();

                if (++m_Count >= 5)
                {
                    DoExpire();
                    m_Mobile.SendLocalizedMessage(1070824); // The creature's rage subsides.
                }
            }
        }
    }
}
