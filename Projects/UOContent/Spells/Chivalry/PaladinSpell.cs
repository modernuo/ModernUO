using System;

namespace Server.Spells.Chivalry
{
    public abstract class PaladinSpell : Spell
    {
        public PaladinSpell(Mobile caster, Item scroll, SpellInfo info) : base(caster, scroll, info)
        {
        }

        public abstract double RequiredSkill { get; }
        public abstract int RequiredMana { get; }
        public abstract int RequiredTithing { get; }
        public abstract int MantraNumber { get; }

        public override SkillName CastSkill => SkillName.Chivalry;
        public override SkillName DamageSkill => SkillName.Chivalry;

        public override bool ClearHandsOnCast => false;

        public override TimeSpan CastDelayMinimum => TimeSpan.FromSeconds(Core.SA ? 0.5 : 0.25);

        public override int CastRecoveryBase => 7;

        public override bool CheckCast()
        {
            var mana = ScaleMana(RequiredMana);

            if (!base.CheckCast())
            {
                return false;
            }

            if (Caster.TithingPoints < RequiredTithing)
            {
                // You must have at least ~1_TITHE_REQUIREMENT~ Tithing Points to use this ability,
                Caster.SendLocalizedMessage(1060173, RequiredTithing.ToString());
                return false;
            }

            if (Caster.Mana < mana)
            {
                // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                Caster.SendLocalizedMessage(1060174, mana.ToString());
                return false;
            }

            return true;
        }

        public override bool CheckFizzle()
        {
            var requiredTithing = RequiredTithing;

            if (AosAttributes.GetValue(Caster, AosAttribute.LowerRegCost) > Utility.Random(100))
            {
                requiredTithing = 0;
            }

            var mana = ScaleMana(RequiredMana);

            if (Caster.TithingPoints < requiredTithing)
            {
                // You must have at least ~1_TITHE_REQUIREMENT~ Tithing Points to use this ability,
                Caster.SendLocalizedMessage(1060173, RequiredTithing.ToString());
                return false;
            }

            if (Caster.Mana < mana)
            {
                // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                Caster.SendLocalizedMessage(1060174, mana.ToString());
                return false;
            }

            Caster.TithingPoints -= requiredTithing;

            if (!base.CheckFizzle())
            {
                return false;
            }

            Caster.Mana -= mana;

            return true;
        }

        public override void SayMantra()
        {
            Caster.PublicOverheadMessage(MessageType.Regular, 0x3B2, MantraNumber, "", false);
        }

        public override void DoFizzle()
        {
            Caster.PlaySound(0x1D6);
            Caster.NextSpellTime = Core.TickCount;
        }

        public override void DoHurtFizzle()
        {
            Caster.PlaySound(0x1D6);
        }

        public override void OnDisturb(DisturbType type, bool message)
        {
            base.OnDisturb(type, message);

            if (message)
            {
                Caster.PlaySound(0x1D6);
            }
        }

        public override void OnBeginCast()
        {
            base.OnBeginCast();

            SendCastEffect();
        }

        public virtual void SendCastEffect()
        {
            Caster.FixedEffect(0x37C4, 10, (int)(GetCastDelay().TotalSeconds * 28), 4, 3);
        }

        public override void GetCastSkills(out double min, out double max)
        {
            min = RequiredSkill;
            max = RequiredSkill + 50.0;
        }

        public override int GetMana() => 0;

        public int ComputePowerValue(int div) => ComputePowerValue(Caster, div);

        public static int ComputePowerValue(Mobile from, int div)
        {
            if (from == null)
            {
                return 0;
            }

            var v = (int)Math.Sqrt(from.Karma + 20000 + from.Skills.Chivalry.Fixed * 10);

            return v / div;
        }
    }
}
