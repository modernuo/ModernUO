using System;
using Server.Items;

namespace Server.Spells
{
  public abstract class MagerySpell : Spell
  {
    private const double ChanceOffset = 20.0, ChanceLength = 100.0 / 7.0;

    private static int[] m_ManaTable = { 4, 6, 9, 11, 14, 20, 40, 50 };

    public MagerySpell(Mobile caster, Item scroll, SpellInfo info)
      : base(caster, scroll, info)
    {
    }

    public abstract SpellCircle Circle{ get; }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds((3 + (int)Circle) * CastDelaySecondsPerTick);

    public override bool ConsumeReagents()
    {
      return base.ConsumeReagents() || ArcaneGem.ConsumeCharges(Caster, Core.SE ? 1 : 1 + (int)Circle);
    }

    public override void GetCastSkills(out double min, out double max)
    {
      int circle = (int)Circle;

      if (Scroll != null)
        circle -= 2;

      double avg = ChanceLength * circle;

      min = avg - ChanceOffset;
      max = avg + ChanceOffset;
    }

    public override int GetMana()
    {
      return Scroll is BaseWand ? 0 : m_ManaTable[(int)Circle];
    }

    public override double GetResistSkill(Mobile m)
    {
      int maxSkill = (1 + (int)Circle) * 100 + (1 + (int)Circle / 6) * 250;

      if (m.Skills.MagicResist.Fixed < maxSkill)
        m.CheckSkill(SkillName.MagicResist, 0, m.Skills.MagicResist.CapFixedPoint);

      return m.Skills.MagicResist.Fixed / 10.0;
    }

    public virtual bool CheckResisted(Mobile target)
    {
      double n = GetResistPercent(target);

      n /= 100.0;

      if (n <= 0.0)
        return false;

      if (n >= 1.0)
        return true;

      int maxSkill = (1 + (int)Circle) * 100 + (1 + (int)Circle / 6) * 250;

      if (target.Skills.MagicResist.Fixed < maxSkill)
        target.CheckSkill(SkillName.MagicResist, 0, target.Skills.MagicResist.CapFixedPoint);

      return n >= Utility.RandomDouble();
    }

    public virtual double GetResistPercentForCircle(Mobile target, SpellCircle circle)
    {
      int firstPercent = target.Skills.MagicResist.Fixed / 50;
      int secondPercent = target.Skills.MagicResist.Fixed -
                             ((Caster.Skills[CastSkill].Fixed - 200) / 50 + (1 + (int)circle) * 50);

      return (firstPercent > secondPercent ? firstPercent : secondPercent) /
             20.0; // Seems should be about half of what stratics says.
    }

    public virtual double GetResistPercent(Mobile target)
    {
      return GetResistPercentForCircle(target, Circle);
    }

    public override TimeSpan GetCastDelay()
    {
      if (!Core.ML && Scroll is BaseWand)
        return TimeSpan.Zero;

      if (!Core.AOS)
        return TimeSpan.FromSeconds(0.5 + 0.25 * (int)Circle);

      return base.GetCastDelay();
    }
  }
}
