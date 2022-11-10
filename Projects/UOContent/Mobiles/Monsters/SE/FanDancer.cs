using System;
using System.Collections.Generic;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    public class FanDancer : BaseCreature
    {
        private static readonly HashSet<Mobile> m_Table = new();

        [Constructible]
        public FanDancer() : base(AIType.AI_Melee)
        {
            Body = 247;
            BaseSoundID = 0x372;

            SetStr(301, 375);
            SetDex(201, 255);
            SetInt(21, 25);

            SetHits(351, 430);

            SetDamage(12, 17);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Fire, 10);
            SetDamageType(ResistanceType.Cold, 10);
            SetDamageType(ResistanceType.Poison, 10);

            SetResistance(ResistanceType.Physical, 40, 60);
            SetResistance(ResistanceType.Fire, 50, 70);
            SetResistance(ResistanceType.Cold, 50, 70);
            SetResistance(ResistanceType.Poison, 50, 70);
            SetResistance(ResistanceType.Energy, 40, 60);

            SetSkill(SkillName.MagicResist, 100.1, 110.0);
            SetSkill(SkillName.Tactics, 85.1, 95.0);
            SetSkill(SkillName.Wrestling, 85.1, 95.0);
            SetSkill(SkillName.Anatomy, 85.1, 95.0);

            Fame = 9000;
            Karma = -9000;

            if (Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }

            AddItem(new Tessen());

            if (Utility.RandomDouble() < 0.02)
            {
                PackItem(new OrigamiPaper());
            }
        }

        public FanDancer(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a fan dancer corpse";
        public override string DefaultName => "a fan dancer";

        public override bool Uncalmable => true;

        private static MonsterAbility[] _abilities = { new ReflectPhysicalDamage() };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Gems, 2);
        }

        private void ThrowFan(Mobile to)
        {
            if (!(Utility.RandomDouble() < 0.8) || to.InRange(this, 1))
            {
                return;
            }

            /* Fan Throw
             * Effect: - To: "0x57D4F5B" - ItemId: "0x27A3" - ItemIdName: "Tessen" - FromLocation: "(992 299, 24)" - ToLocation: "(992 308, 22)" - Speed: "10" - Duration: "0" - FixedDirection: "False" - Explode: "False" - Hue: "0x0" - Render: "0x0"
             * Damage: 50-65
             */
            Effects.SendMovingEffect(
                to.Location,
                to.Map,
                0x27A3,
                Location,
                to.Location,
                10,
                0
            );

            AOS.Damage(to, this, Utility.RandomMinMax(50, 65), 100, 0, 0, 0, 0);
        }

        public override void OnDamagedBySpell(Mobile attacker, int damage)
        {
            base.OnDamagedBySpell(attacker, damage);
            ThrowFan(attacker);
        }

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);
            ThrowFan(attacker);
        }

        public override void OnGaveMeleeAttack(Mobile defender, int damage)
        {
            base.OnGaveMeleeAttack(defender, damage);

            if (m_Table.Add(defender) && Utility.RandomDouble() < 0.05)
            {
                /* Fanning Fire
                 * Graphic: Type: "3" From: "0x57D4F5B" To: "0x0" ItemId: "0x3709" ItemIdName: "fire column" FromLocation: "(994 325, 16)" ToLocation: "(994 325, 16)" Speed: "10" Duration: "30" FixedDirection: "True" Explode: "False" Hue: "0x0" RenderMode: "0x0" Effect: "0x34" ExplodeEffect: "0x1" ExplodeSound: "0x0" Serial: "0x57D4F5B" Layer: "5" Unknown: "0x0"
                 * Sound: 0x208
                 * Start cliloc: 1070833
                 * Effect: Fire res -10% for 10 seconds
                 * Damage: 35-45, 100% fire
                 * End cliloc: 1070834
                 * Effect does not stack
                 */

                // The creature fans you with fire, reducing your resistance to fire attacks.
                defender.SendLocalizedMessage(1070833);

                var effect = -(defender.FireResistance / 10);

                var mod = new ResistanceMod(ResistanceType.Fire, "FireResistFanningFire", effect);

                defender.FixedParticles(0x37B9, 10, 30, 0x34, EffectLayer.RightFoot);
                defender.PlaySound(0x208);

                // This should be done in place of the normal attack damage.
                // AOS.Damage( defender, this, Utility.RandomMinMax( 35, 45 ), 0, 100, 0, 0, 0 );

                defender.AddResistanceMod(mod);

                var timer = new ExpireTimer(defender, mod, TimeSpan.FromSeconds(10.0));
                timer.Start();
            }
        }

        public static bool IsFanned(Mobile m) => m_Table.Contains(m);

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
            private readonly Mobile m_Mobile;
            private readonly ResistanceMod m_Mod;

            public ExpireTimer(Mobile m, ResistanceMod mod, TimeSpan delay) : base(delay)
            {
                m_Mobile = m;
                m_Mod = mod;
            }

            protected override void OnTick()
            {
                m_Mobile.SendLocalizedMessage(1070834); // Your resistance to fire attacks has returned.
                m_Mobile.RemoveResistanceMod(m_Mod);
                Stop();
                m_Table.Remove(m_Mobile);
            }
        }
    }
}
