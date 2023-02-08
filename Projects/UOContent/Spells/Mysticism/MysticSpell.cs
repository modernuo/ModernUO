using System;
using Server.Items;

namespace Server.Spells.Mysticism
{
    public abstract class MysticSpell : Spell
    {
        private static int[] _manaTable = { 4, 6, 9, 11, 14, 20, 40, 50 };
        private static double[] _requiredSkill = { 0.0, 8.0, 20.0, 33.0, 45.0, 58.0, 70.0, 83.0 };

        public MysticSpell(Mobile caster, Item scroll, SpellInfo info) : base(caster, scroll, info)
        {
        }

        public abstract SpellCircle Circle { get; }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.5 + 0.25 * (int)Circle);

        public virtual double RequiredSkill
        {
            get
            {
                var circle = (int)Circle;

                if (Scroll != null)
                {
                    circle -= 2;
                }

                return _requiredSkill[circle];
            }
        }

        public override SkillName CastSkill => SkillName.Mysticism;

        /*
         * As per OSI Publish 64:
         * Imbuing is not the only skill associated with Mysticism now.
         * Players can use EITHER their Focus skill or Imbuing skill.
         * Evaluate Intelligence no longer has any effect on a Mystic’s spell power.
         */
        public override double GetDamageSkill(Mobile m) => Math.Max(m.Skills.Imbuing.Value, m.Skills.Focus.Value);

        public override int GetDamageFixed(Mobile m) => Math.Max(m.Skills.Imbuing.Fixed, m.Skills.Focus.Fixed);

        public override void GetCastSkills(out double min, out double max)
        {
            var requiredSkill = RequiredSkill;

            // As per Mysticism page at the UO Herald Playguide
            // This means that we have 25% success chance at min Required Skill
            min = requiredSkill - 12.5;
            max = requiredSkill + 37.5;
        }

        public override int GetMana() => Scroll is BaseWand ? 0 : _manaTable[(int)Circle];

        public override bool CheckCast()
        {
            if (!base.CheckCast())
            {
                return false;
            }

            var mana = ScaleMana(GetMana());

            if (Caster.Mana < mana)
            {
                // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                Caster.SendLocalizedMessage(1060174, mana.ToString());
                return false;
            }

            var requiredSkill = RequiredSkill;

            if (Caster.Skills[CastSkill].Value < requiredSkill)
            {
                // You need at least ~1_SKILL_REQUIREMENT~ ~2_SKILL_NAME~ skill to use that ability.
                Caster.SendLocalizedMessage(1063013, $"{requiredSkill:F1}\t{CastSkill}\t ");
                return false;
            }

            return true;
        }

        public override void OnBeginCast()
        {
            base.OnBeginCast();

            SendCastEffect();
        }

        public virtual void SendCastEffect()
        {
            Caster.FixedEffect(0x37C4, 10, (int)(GetCastDelay().TotalSeconds * 28), 0x66C, 3);
        }

        public static double GetBaseSkill(Mobile m) => m.Skills.Mysticism.Value;

        public virtual bool CheckResisted(Mobile target)
        {
            var n = GetResistPercent(target);

            n /= 100.0;

            if (n <= 0.0)
            {
                return false;
            }

            if (n >= 1.0)
            {
                return true;
            }

            var maxSkill = (1 + (int)Circle) * 10;
            maxSkill += (1 + (int)Circle / 6) * 25;

            if (target.Skills.MagicResist.Value < maxSkill)
            {
                target.CheckSkill(SkillName.MagicResist, 0.0, target.Skills.MagicResist.Cap);
            }

            return n >= Utility.RandomDouble();
        }

        public virtual double GetResistPercentForCircle(Mobile target, SpellCircle circle)
        {
            var magicResist = target.Skills.MagicResist.Value;
            var firstPercent = magicResist / 5.0;
            var secondPercent = magicResist -
                                ((Caster.Skills[CastSkill].Value - 20.0) / 5.0 + (1 + (int)circle) * 5.0);

            // Seems should be about half of what stratics says.
            return (firstPercent > secondPercent ? firstPercent : secondPercent) / 2.0;
        }

        public virtual double GetResistPercent(Mobile target) => GetResistPercentForCircle(target, Circle);
    }
}
