using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Sixth
{
  public class RevealSpell : MagerySpell, ISpellTargetingPoint3D
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Reveal", "Wis Quas",
      206,
      9002,
      Reagent.Bloodmoss,
      Reagent.SulfurousAsh);

    public RevealSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Sixth;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetPoint3D(this, TargetFlags.None, Core.ML ? 10 : 12);
    }

    public void Target(IPoint3D p)
    {
      if (!Caster.CanSee(p))
      {
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      }
      else if (CheckSequence())
      {
        SpellHelper.Turn(Caster, p);
        SpellHelper.GetSurfaceTop(ref p);
        Map map = Caster.Map;

        if (map != null)
        {
          IPooledEnumerable<Mobile> eable = map.GetMobilesInRange(new Point3D(p),
            1 + (int)(Caster.Skills.Magery.Value / 20.0));

          foreach (Mobile m in eable)
          {
            if (m is ShadowKnight &&
                (m.X != p.X || m.Y != p.Y || !m.Hidden || (m.AccessLevel != AccessLevel.Player &&
                 Caster.AccessLevel <= m.AccessLevel) ||
                 !CheckDifficulty(Caster, m)))
              continue;

            m.RevealingAction();

            m.FixedParticles(0x375A, 9, 20, 5049, EffectLayer.Head);
            m.PlaySound(0x1FD);
          }

          eable.Free();
        }
      }

      FinishSequence();
    }

    // Reveal uses magery and detect hidden vs. hide and stealth
    private static bool CheckDifficulty(Mobile from, Mobile m)
    {
      // Reveal always reveals vs. invisibility spell
      if (!Core.AOS || InvisibilitySpell.HasTimer(m))
        return true;

      int magery = from.Skills.Magery.Fixed;
      int detectHidden = from.Skills.DetectHidden.Fixed;

      int hiding = m.Skills.Hiding.Fixed;
      int stealth = m.Skills.Stealth.Fixed;
      int divisor = hiding + stealth;

      int chance;
      if (divisor > 0)
        chance = 50 * (magery + detectHidden) / divisor;
      else
        chance = 100;

      return chance > Utility.Random(100);
    }
  }
}
