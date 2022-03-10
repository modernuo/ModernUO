using System;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    public class Betrayer : BaseCreature
    {
        private DateTime m_NextAbilityTime;
        private bool m_Stunning;

        [Constructible]
        public Betrayer() : base(AIType.AI_Mage)
        {
            Body = 767;

            SetStr(401, 500);
            SetDex(81, 100);
            SetInt(151, 200);

            SetHits(241, 300);

            SetDamage(16, 22);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 60, 70);
            SetResistance(ResistanceType.Fire, 60, 70);
            SetResistance(ResistanceType.Cold, 60, 70);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Anatomy, 90.1, 100.0);
            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 50.1, 100.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 120.1, 130.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 65;
            SpeechHue = Utility.RandomDyedHue();

            PackItem(new PowerCrystal());

            if (Utility.RandomDouble() < 0.02)
            {
                PackItem(new BlackthornWelcomeBook());
            }

            m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(5, 30));
        }

        public Betrayer(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a betrayer corpse";

        public override string DefaultName => "a betrayer";

        public override bool AlwaysMurderer => true;
        public override bool BardImmune => !Core.AOS;
        public override Poison PoisonImmune => Poison.Lethal;
        public override int Meat => 1;
        public override int TreasureMapLevel => 5;

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (Utility.RandomDouble() < 0.05)
            {
                if (!IsParagon)
                {
                    if (Utility.RandomDouble() < 0.75)
                    {
                        c.DropItem(DawnsMusicGear.RandomCommon);
                    }
                    else
                    {
                        c.DropItem(DawnsMusicGear.RandomUncommon);
                    }
                }
                else
                {
                    c.DropItem(DawnsMusicGear.RandomRare);
                }
            }
        }

        public override int GetDeathSound() => 0x423;

        public override int GetAttackSound() => 0x23B;

        public override int GetHurtSound() => 0x140;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Gems, 1);
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (!m_Stunning && Utility.RandomDouble() < 0.3)
            {
                m_Stunning = true;

                defender.Animate(21, 6, 1, true, false, 0);
                PlaySound(0xEE);
                defender.LocalOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    false,
                    "You have been stunned by a colossal blow!"
                );

                if (Weapon is BaseWeapon weapon)
                {
                    weapon.OnHit(this, defender);
                }

                if (defender.Alive)
                {
                    defender.Frozen = true;
                    Timer.StartTimer(TimeSpan.FromSeconds(5.0), () => Recover_Callback(defender));
                }
            }
        }

        private void Recover_Callback(Mobile defender)
        {
            defender.Frozen = false;
            defender.Combatant = null;
            defender.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You recover your senses.");
            m_Stunning = false;
        }

        public override void OnActionCombat()
        {
            var combatant = Combatant;

            if (Core.Now < m_NextAbilityTime || combatant?.Deleted != false || combatant.Map != Map ||
                !InRange(combatant, 3) || !CanBeHarmful(combatant) || !InLOS(combatant))
            {
                return;
            }

            m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(5, 30));

            if (Utility.RandomBool())
            {
                FixedParticles(0x376A, 9, 32, 0x2539, EffectLayer.LeftHand);
                PlaySound(0x1DE);

                foreach (var m in GetMobilesInRange(2))
                {
                    if (m != this && IsEnemy(m))
                    {
                        m.ApplyPoison(this, Poison.Deadly);
                    }
                }
            }
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
    }
}
