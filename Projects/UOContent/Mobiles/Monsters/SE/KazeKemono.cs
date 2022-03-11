using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class KazeKemono : BaseCreature
    {
        private static readonly Dictionary<Mobile, ExpireTimer> m_FlurryOfTwigsTable = new();

        private static readonly Dictionary<Mobile, ExpireTimer> m_ChlorophylBlastTable =
            new();

        [Constructible]
        public KazeKemono()
            : base(AIType.AI_Mage)
        {
            Body = 196;
            BaseSoundID = 655;

            SetStr(201, 275);
            SetDex(101, 155);
            SetInt(101, 105);

            SetHits(251, 330);

            SetDamage(15, 20);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Fire, 10);
            SetDamageType(ResistanceType.Cold, 10);
            SetDamageType(ResistanceType.Poison, 10);

            SetResistance(ResistanceType.Physical, 50, 70);
            SetResistance(ResistanceType.Fire, 30, 60);
            SetResistance(ResistanceType.Cold, 30, 60);
            SetResistance(ResistanceType.Poison, 50, 70);
            SetResistance(ResistanceType.Energy, 60, 80);

            SetSkill(SkillName.MagicResist, 110.1, 125.0);
            SetSkill(SkillName.Tactics, 55.1, 65.0);
            SetSkill(SkillName.Wrestling, 85.1, 95.0);
            SetSkill(SkillName.Anatomy, 25.1, 35.0);
            SetSkill(SkillName.Magery, 95.1, 105.0);

            Fame = 8000;
            Karma = -8000;
        }

        public KazeKemono(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a kaze kemono corpse";
        public override string DefaultName => "a kaze kemono";

        public override bool BleedImmune => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich, 3);
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (Utility.RandomDouble() < 0.1)
            {
                /* Flurry of Twigs
                 * Start cliloc: 1070850
                 * Effect: Physical resistance -15% for 5 seconds
                 * End cliloc: 1070852
                 * Effect: Type: "3" From: "0x57D4F5B" To: "0x0" ItemId: "0x37B9" ItemIdName: "glow" FromLocation: "(1048 779, 6)" ToLocation: "(1048 779, 6)" Speed: "10" Duration: "5" FixedDirection: "True" Explode: "False"
                 */

                if (m_FlurryOfTwigsTable.TryGetValue(defender, out var timer))
                {
                    timer.DoExpire();
                    defender.SendLocalizedMessage(1070851); // The creature lands another blow in your weakened state.
                }
                else
                {
                    defender.SendLocalizedMessage(
                        1070850
                    ); // The creature's flurry of twigs has made you more susceptible to physical attacks!
                }

                var effect = -(defender.PhysicalResistance * 15 / 100);

                var mod = new ResistanceMod(ResistanceType.Physical, effect);

                defender.FixedEffect(0x37B9, 10, 5);
                defender.AddResistanceMod(mod);

                timer = new ExpireTimer(defender, mod, m_FlurryOfTwigsTable, TimeSpan.FromSeconds(5.0));
                timer.Start();
                m_FlurryOfTwigsTable[defender] = timer;
                return;
            }

            if (Utility.RandomDouble() < 0.05)
            {
                /* Chlorophyl Blast
                 * Start cliloc: 1070827
                 * Effect: Energy resistance -50% for 10 seconds
                 * End cliloc: 1070829
                 * Effect: Type: "3" From: "0x57D4F5B" To: "0x0" ItemId: "0x37B9" ItemIdName: "glow" FromLocation: "(1048 779, 6)" ToLocation: "(1048 779, 6)" Speed: "10" Duration: "5" FixedDirection: "True" Explode: "False"
                 */

                if (m_ChlorophylBlastTable.TryGetValue(defender, out var timer))
                {
                    timer.DoExpire();
                    defender.SendLocalizedMessage(1070828); // The creature continues to hinder your energy resistance!
                }
                else
                {
                    defender.SendLocalizedMessage(
                        1070827
                    ); // The creature's attack has made you more susceptible to energy attacks!
                }

                var effect = -(defender.EnergyResistance / 2);

                var mod = new ResistanceMod(ResistanceType.Energy, effect);

                defender.FixedEffect(0x37B9, 10, 5);
                defender.AddResistanceMod(mod);

                timer = new ExpireTimer(defender, mod, m_ChlorophylBlastTable, TimeSpan.FromSeconds(10.0));
                timer.Start();
                m_ChlorophylBlastTable[defender] = timer;
            }
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

        private class ExpireTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly ResistanceMod m_Mod;
            private readonly Dictionary<Mobile, ExpireTimer> m_Table;

            public ExpireTimer(Mobile m, ResistanceMod mod, Dictionary<Mobile, ExpireTimer> table, TimeSpan delay)
                : base(delay)
            {
                m_Mobile = m;
                m_Mod = mod;
                m_Table = table;
            }

            public void DoExpire()
            {
                m_Mobile.RemoveResistanceMod(m_Mod);
                Stop();
                m_Table.Remove(m_Mobile);
            }

            protected override void OnTick()
            {
                if (m_Mod.Type == ResistanceType.Physical)
                {
                    m_Mobile.SendLocalizedMessage(1070852); // Your resistance to physical attacks has returned.
                }
                else
                {
                    m_Mobile.SendLocalizedMessage(1070829); // Your resistance to energy attacks has returned.
                }

                DoExpire();
            }
        }
    }
}
