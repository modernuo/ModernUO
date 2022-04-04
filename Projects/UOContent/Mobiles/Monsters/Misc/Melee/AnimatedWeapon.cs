using System;

namespace Server.Mobiles
{
    public class AnimatedWeapon : BaseCreature
    {
        [Constructible]
        public AnimatedWeapon(Mobile caster, int level) : base(AIType.AI_Melee)
        {
            Body = 692;

            SetStr(10 + level);
            SetDex(10 + level);
            SetInt(10);

            SetHits(20 + level * 3 / 2);
            SetStam(10 + level);
            SetMana(0);

            if (level >= 120)
            {
                SetDamage(14, 18);
            }
            else if (level >= 105)
            {
                SetDamage(13, 17);
            }
            else if (level >= 90)
            {
                SetDamage(12, 15);
            }
            else if (level >= 75)
            {
                SetDamage(11, 14);
            }
            else if (level >= 60)
            {
                SetDamage(10, 12);
            }
            else if (level >= 45)
            {
                SetDamage(9, 11);
            }
            else if (level >= 30)
            {
                SetDamage(8, 9);
            }
            else
            {
                SetDamage(7, 8);
            }

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Poison, 20);
            SetDamageType(ResistanceType.Energy, 20);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.MagicResist, level);
            SetSkill(SkillName.Wrestling, level);
            SetSkill(SkillName.Anatomy, caster.Skills.Anatomy.Value / 2);
            SetSkill(SkillName.Tactics, caster.Skills.Tactics.Value / 2);

            Fame = 0;
            Karma = 0;

            ControlSlots = 4;
        }

        public AnimatedWeapon(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "an animated weapon corpse";
        public override bool DeleteCorpseOnDeath => true;
        public override bool IsHouseSummonable => true;

        public override double DispelDifficulty => 0.0;
        public override double DispelFocus => 20.0;

        public override string DefaultName => "an animated weapon";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;

        public override double GetFightModeRanking(Mobile m, FightMode acqType, bool bPlayerOnly) =>
            m.Str / Math.Max(GetDistanceToSqrt(m), 1.0);

        public override int GetAngerSound() => 0x23A;

        public override int GetAttackSound() => 0x3B8;

        public override int GetHurtSound() => 0x23A;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            /*int version = */
            reader.ReadInt();
        }
    }
}
