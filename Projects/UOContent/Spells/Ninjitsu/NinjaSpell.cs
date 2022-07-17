using Server.Mobiles;

namespace Server.Spells.Ninjitsu
{
    public abstract class NinjaSpell : Spell
    {
        public NinjaSpell(Mobile caster, Item scroll, SpellInfo info) : base(caster, scroll, info)
        {
        }

        public abstract double RequiredSkill { get; }
        public abstract int RequiredMana { get; }

        public override SkillName CastSkill => SkillName.Ninjitsu;
        public override SkillName DamageSkill => SkillName.Ninjitsu;

        public override bool RevealOnCast => false;
        public override bool ClearHandsOnCast => false;
        public override bool ShowHandMovement => false;

        public override bool BlocksMovement => false;

        // public override int CastDelayBase => 1;

        public override int CastRecoveryBase => 7;

        public static bool CheckExpansion(Mobile from) =>
            (from as PlayerMobile)?.NetState?.SupportsExpansion(Expansion.SE) == true;

        public override bool CheckCast()
        {
            var mana = ScaleMana(RequiredMana);

            if (!base.CheckCast())
            {
                return false;
            }

            if (!CheckExpansion(Caster))
            {
                Caster.SendLocalizedMessage(1063456); // You must upgrade to Samurai Empire in order to use that ability.
                return false;
            }

            if (Caster.Skills[CastSkill].Value < RequiredSkill)
            {
                // You need at least ~1_SKILL_REQUIREMENT~ ~2_SKILL_NAME~ skill to use that ability.
                Caster.SendLocalizedMessage(1063013, $"{RequiredSkill:F1}\t{CastSkill}\t ");
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
            var mana = ScaleMana(RequiredMana);

            if (Caster.Skills[CastSkill].Value < RequiredSkill)
            {
                // You need ~1_SKILL_REQUIREMENT~ Ninjitsu skill to perform that attack!
                Caster.SendLocalizedMessage(1063352, RequiredSkill.ToString("F1"));
                return false;
            }

            if (Caster.Mana < mana)
            {
                // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                Caster.SendLocalizedMessage(1060174, mana.ToString());
                return false;
            }

            if (!base.CheckFizzle())
            {
                return false;
            }

            Caster.Mana -= mana;

            return true;
        }

        public override void GetCastSkills(out double min, out double max)
        {
            min = RequiredSkill - 12.5; // Per 5 on friday 2/16/07
            max = RequiredSkill + 37.5;
        }

        public override int GetMana() => 0;
    }
}
