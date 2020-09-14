using System;
using Server.Items;

namespace Server.Spells
{
    public abstract class MagerySpell : Spell
    {
        private const double ChanceOffset = 20.0, ChanceLength = 100.0 / 7.0;

        private static readonly int[] m_ManaTable = { 4, 6, 9, 11, 14, 20, 40, 50 };

        public MagerySpell(Mobile caster, Item scroll, SpellInfo info)
            : base(caster, scroll, info)
        {
        }

        public abstract SpellCircle Circle { get; }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds((3 + (int)Circle) * CastDelaySecondsPerTick);

        public override bool ConsumeReagents() =>
            base.ConsumeReagents() || ArcaneGem.ConsumeCharges(Caster, Core.SE ? 1 : 1 + (int)Circle);

        public override void GetCastSkills(out double min, out double max)
        {
            var circle = (int)Circle;

            if (Scroll != null)
            {
                circle -= 2;
            }

            var avg = ChanceLength * circle;

            min = avg - ChanceOffset;
            max = avg + ChanceOffset;
        }

        public override int GetMana() => Scroll is BaseWand ? 0 : m_ManaTable[(int)Circle];

        public override double GetResistSkill(Mobile m)
        {
            var maxSkill = (1 + (int)Circle) * 10;
            maxSkill += (1 + (int)Circle / 6) * 25;

            if (m.Skills.MagicResist.Value < maxSkill)
            {
                m.CheckSkill(SkillName.MagicResist, 0.0, m.Skills.MagicResist.Cap);
            }

            return m.Skills.MagicResist.Value;
        }

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
            var firstPercent = target.Skills.MagicResist.Value / 5.0;
            var secondPercent = target.Skills.MagicResist.Value -
                                ((Caster.Skills[CastSkill].Value - 20.0) / 5.0 + (1 + (int)circle) * 5.0);

            return (firstPercent > secondPercent ? firstPercent : secondPercent) /
                   2.0; // Seems should be about half of what stratics says.
        }

        public virtual double GetResistPercent(Mobile target) => GetResistPercentForCircle(target, Circle);

        public override TimeSpan GetCastDelay() =>
            !Core.ML && Scroll is BaseWand ? TimeSpan.Zero :
            !Core.AOS ? TimeSpan.FromSeconds(0.5 + 0.25 * (int)Circle) : base.GetCastDelay();
    }
}
