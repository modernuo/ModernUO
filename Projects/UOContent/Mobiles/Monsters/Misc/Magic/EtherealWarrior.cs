using System;
using Server.Gumps;

namespace Server.Mobiles
{
    public class EtherealWarrior : BaseCreature
    {
        private static readonly TimeSpan ResurrectDelay = TimeSpan.FromSeconds(2.0);

        private DateTime m_NextResurrect;

        [Constructible]
        public EtherealWarrior() : base(AIType.AI_Mage, FightMode.Evil)
        {
            Name = NameList.RandomName("ethereal warrior");
            Body = 123;

            SetStr(586, 785);
            SetDex(177, 255);
            SetInt(351, 450);

            SetHits(352, 471);

            SetDamage(13, 19);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 80, 90);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.Anatomy, 50.1, 75.0);
            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 99.1, 100.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 7000;
            Karma = 7000;

            VirtualArmor = 120;
        }

        public EtherealWarrior(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an ethereal warrior corpse";
        public override bool InitialInnocent => true;

        public override int TreasureMapLevel => Core.AOS ? 5 : 0;

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override int Feathers => 100;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich, 3);
            AddLoot(LootPack.Gems);
        }

        public override void OnMovement(Mobile from, Point3D oldLocation)
        {
            if (!from.Alive && from is PlayerMobile)
            {
                if (!from.Frozen && Core.Now >= m_NextResurrect && InRange(from, 4) && !InRange(oldLocation, 4) &&
                    InLOS(from))
                {
                    m_NextResurrect = Core.Now + ResurrectDelay;
                    if (!from.Criminal && from.Kills < 5 && from.Karma > 0)
                    {
                        if (from.Map?.CanFit(from.Location, 16, false, false) == true)
                        {
                            Direction = GetDirectionTo(from);
                            from.PlaySound(0x1F2);
                            from.FixedEffect(0x376A, 10, 16);
                            from.CloseGump<ResurrectGump>();
                            from.SendGump(new ResurrectGump(from, ResurrectMessage.Healer));
                        }
                    }
                }
            }
        }

        public override int GetAngerSound() => 0x2F8;

        public override int GetIdleSound() => 0x2F8;

        public override int GetAttackSound() => Utility.Random(0x2F5, 2);

        public override int GetHurtSound() => 0x2F9;

        public override int GetDeathSound() => 0x2F7;

        public override void OnGaveMeleeAttack(Mobile defender, int damage)
        {
            base.OnGaveMeleeAttack(defender, damage);

            defender.Damage(Utility.Random(10, 10), this);
            defender.Stam -= Utility.Random(10, 10);
            defender.Mana -= Utility.Random(10, 10);
        }

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);

            attacker.Damage(Utility.Random(10, 10), this);
            attacker.Stam -= Utility.Random(10, 10);
            attacker.Mana -= Utility.Random(10, 10);
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
