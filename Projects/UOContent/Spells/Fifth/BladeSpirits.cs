using System;
using Server.Mobiles;

namespace Server.Spells.Fifth
{
  public class BladeSpiritsSpell : MagerySpell, ISpellTargetingPoint3D
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Blade Spirits", "In Jux Hur Ylem",
      266,
      9040,
      false,
      Reagent.BlackPearl,
      Reagent.MandrakeRoot,
      Reagent.Nightshade);

    public BladeSpiritsSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fifth;

    public override TimeSpan GetCastDelay()
    {
      if (Core.AOS)
        return TimeSpan.FromTicks(base.GetCastDelay().Ticks * (Core.SE ? 3 : 5));

      return base.GetCastDelay() + TimeSpan.FromSeconds(6.0);
    }

    public override bool CheckCast()
    {
      if (!base.CheckCast())
        return false;

      if (Caster.Followers + (Core.SE ? 2 : 1) > Caster.FollowersMax)
      {
        Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
        return false;
      }

      return true;
    }

    public override void OnCast()
    {
      Caster.Target = new SpellTargetPoint3D(this);
    }

    public void Target(IPoint3D p)
    {
      Map map = Caster.Map;

      SpellHelper.GetSurfaceTop(ref p);

      if (map?.CanSpawnMobile(p.X, p.Y, p.Z) != true)
      {
        Caster.SendLocalizedMessage(501942); // That location is blocked.
      }
      else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
      {
        TimeSpan duration;

        if (Core.AOS)
          duration = TimeSpan.FromSeconds(120);
        else
          duration = TimeSpan.FromSeconds(Utility.Random(80, 40));

        BaseCreature.Summon(new BladeSpirits(), false, Caster, new Point3D(p), 0x212, duration);
      }

      FinishSequence();
    }
  }
}
