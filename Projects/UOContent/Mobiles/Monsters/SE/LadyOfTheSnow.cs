using System;
using System.Collections.Generic;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    public class LadyOfTheSnow : BaseCreature
    {
        private static readonly Dictionary<Mobile, ExpireTimer> m_Table = new();

        [Constructible]
        public LadyOfTheSnow()
            : base(AIType.AI_Mage)
        {
            Body = 252;
            BaseSoundID = 0x482;

            SetStr(276, 305);
            SetDex(106, 125);
            SetInt(471, 495);

            SetHits(596, 625);

            SetDamage(13, 20);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Cold, 80);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 40, 55);
            SetResistance(ResistanceType.Cold, 70, 90);
            SetResistance(ResistanceType.Poison, 60, 70);
            SetResistance(ResistanceType.Energy, 65, 85);

            SetSkill(SkillName.Magery, 95.1, 110.0);
            SetSkill(SkillName.MagicResist, 90.1, 105.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 80.1, 100.0);
            SetSkill(SkillName.Necromancy, 90, 110.0);
            SetSkill(SkillName.SpiritSpeak, 90.0, 110.0);

            Fame = 15200;
            Karma = -15200;

            PackReg(3);
            PackItem(new Necklace());

            if (Utility.RandomDouble() < 0.25)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }
        }

        public LadyOfTheSnow(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a lady of the snow corpse";
        public override string DefaultName => "a lady of the snow";

        public override bool BleedImmune => true;
        public override bool CanRummageCorpses => true;
        public override int TreasureMapLevel => 4;

        public override int GetDeathSound() => 0x370;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
        }

        // TODO: Snowball

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (Utility.RandomDouble() >= 0.1)
            {
                return;
            }

            /* Cold Wind
             * Graphics: Message - Type: "3" From: "0x57D4F5B" To: "0x0" ItemId: "0x37B9" ItemIdName: "glow" FromLocation: "(928 164, 34)" ToLocation: "(928 164, 34)" Speed: "10" Duration: "5" FixedDirection: "True" Explode: "False"
             * Start cliloc: 1070832
             * Damage: 1hp per second for 5 seconds
             * End cliloc: 1070830
             * Reset cliloc: 1070831
             */

            if (m_Table.TryGetValue(defender, out var timer))
            {
                timer.DoExpire();
                defender.SendLocalizedMessage(1070831); // The freezing wind continues to blow!
            }
            else
            {
                defender.SendLocalizedMessage(1070832); // An icy wind surrounds you, freezing your lungs as you breathe!
            }

            timer = new ExpireTimer(defender, this);
            timer.Start();
            m_Table[defender] = timer;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

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
                    m_Mobile.SendLocalizedMessage(1070830); // The icy wind dissipates.
                }
            }
        }
    }
}
