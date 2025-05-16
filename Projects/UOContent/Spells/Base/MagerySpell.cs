using System;
using Server.Items;

namespace Server.Spells
{
    public abstract class MagerySpell : Spell
    {
        // Mana costs per spell circle
        public static int[] ManaPerCircle { get; set; } = { 4, 6, 9, 11, 14, 20, 40, 50 };

        // Minimum skill required per circle (scrolls use Circle+2) for non-ML core (10 entries)
        public static double[] SkillTable { get; set; } = { -50.0, -30.0, 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0 };
        public static double[] SkillTableML { get; set; } = { -46.0, -32.0, -18.0, -4.0, 10.0, 24.0, 38.0, 52.0, 66.0, 80.0 };

        public static double[] RequiredSkill => Core.ML ? SkillTableML : SkillTable;

        // Skill check window for casting a spell successfully
        public static double SkillCheckWindow { get; set; } = 40.0;

        // Cast delay per tick (seconds)
        public static double CastDelay { get; set; } = 1.0;

        public MagerySpell(Mobile caster, Item scroll, SpellInfo info) : base(caster, scroll, info) { }

        public abstract SpellCircle Circle { get; }

        public override TimeSpan CastDelayBase =>
            TimeSpan.FromSeconds((3 + (int)Circle) * CastDelay);

        public override bool ConsumeReagents() =>
            base.ConsumeReagents() || ArcaneGem.ConsumeCharges(Caster, Core.SE ? 1 : 1 + (int)Circle);

        public override void GetCastSkills(out double min, out double max)
        {
            // Uses scrolls if present, otherwise use spellbook
            var skillIndex = (int)(Scroll == null ? Circle + 2 : Circle);
            min = RequiredSkill[skillIndex];
            max = min + SkillCheckWindow;
        }

        public override int GetMana() => Scroll is BaseWand ? 0 : ManaPerCircle[(int)Circle];

        public override double GetResistSkill(Mobile m)
        {
            var circle = (int)Circle;
            var maxSkill = 1 + circle * 10 + (1 + circle / 6) * 25;

            if (m.Skills.MagicResist.Value < maxSkill)
            {
                m.CheckSkill(SkillName.MagicResist, 0.0, m.Skills.MagicResist.Cap);
            }

            return m.Skills.MagicResist.Value;
        }

        public virtual bool CheckResisted(Mobile target)
        {
            var resistChance = GetResistPercent(target) / 100.0;

            if (resistChance <= 0.0)
            {
                return false;
            }

            if (resistChance >= 1.0)
            {
                return true;
            }

            var circle = (int)Circle;
            var maxSkill = (1 + circle) * 10 + (1 + circle / 6) * 25;

            if (target.Skills.MagicResist.Value < maxSkill)
            {
                target.CheckSkill(SkillName.MagicResist, 0.0, target.Skills.MagicResist.Cap);
            }

            return resistChance >= Utility.RandomDouble();
        }

        public virtual double GetResistPercentForCircle(Mobile target, SpellCircle circle)
        {
            var magicResist = target.Skills.MagicResist.Value;
            var casterSkill = Caster.Skills[CastSkill].Value;

            var firstPercent = magicResist / 5.0;
            var secondPercent = magicResist - ((casterSkill - 20.0) / 5.0 + (1 + (int)circle) * 5.0);

            // Uses the higher of the two, then halves it
            return Math.Max(firstPercent, secondPercent) / 2.0;
        }

        public virtual double GetResistPercent(Mobile target) => GetResistPercentForCircle(target, Circle);

        public override TimeSpan GetCastDelay()
        {
            if (!Core.ML && Scroll is BaseWand)
            {
                return TimeSpan.Zero;
            }

            if (!Core.AOS)
            {
                return TimeSpan.FromSeconds(0.5 + 0.25 * (int)Circle);
            }

            return base.GetCastDelay();
        }
    }
}
