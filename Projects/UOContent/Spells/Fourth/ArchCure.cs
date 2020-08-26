using System;
using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Fourth
{
  public class ArchCureSpell : MagerySpell, ISpellTargetingPoint3D
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Arch Cure", "Vas An Nox",
      215,
      9061,
      Reagent.Garlic,
      Reagent.Ginseng,
      Reagent.MandrakeRoot);

    public ArchCureSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fourth;

    // Arch cure is now 1/4th of a second faster
    public override TimeSpan CastDelayBase => base.CastDelayBase - TimeSpan.FromSeconds(0.25);

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

        List<Mobile> targets = new List<Mobile>();

        Map map = Caster.Map;
        Mobile directTarget = p as Mobile;

        if (map != null)
        {
          bool feluccaRules = map.Rules == MapRules.FeluccaRules;

          // You can target any living mobile directly, beneficial checks apply
          if (directTarget != null && Caster.CanBeBeneficial(directTarget, false))
            targets.Add(directTarget);

          IPooledEnumerable<Mobile> eable = map.GetMobilesInRange(new Point3D(p), 2);
          targets.AddRange(eable.Where(m => m != directTarget).Where(m => AreaCanTarget(m, feluccaRules)));

          eable.Free();
        }

        Effects.PlaySound(p, Caster.Map, 0x299);

        if (targets.Count > 0)
        {
          int cured = 0;

          for (int i = 0; i < targets.Count; ++i)
          {
            Mobile m = targets[i];

            Caster.DoBeneficial(m);

            Poison poison = m.Poison;

            if (poison != null)
            {
              int chanceToCure = 10000 + (int)(Caster.Skills.Magery.Value * 75) -
                                 (poison.Level + 1) * 1750;
              chanceToCure /= 100;
              chanceToCure -= 1;

              if (chanceToCure > Utility.Random(100) && m.CurePoison(Caster))
                ++cured;
            }

            m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
            m.PlaySound(0x1E0);
          }

          if (cured > 0)
            Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!
        }
      }

      FinishSequence();
    }

    private bool AreaCanTarget(Mobile target, bool feluccaRules)
    {
      /* Arch cure area effect won't cure aggressors, victims, murderers, criminals or monsters.
       * In Felucca, it will also not cure summons and pets.
       * For red players it will only cure themselves and guild members.
       */

      if (!Caster.CanBeBeneficial(target, false))
        return false;

      if (Core.AOS && target != Caster)
      {
        if (IsAggressor(target) || IsAggressed(target))
          return false;

        if ((!IsInnocentTo(Caster, target) || !IsInnocentTo(target, Caster)) && !IsAllyTo(Caster, target))
          return false;

        if (feluccaRules && !(target is PlayerMobile))
          return false;
      }

      return true;
    }

    private bool IsAggressor(Mobile m)
    {
      foreach (AggressorInfo info in Caster.Aggressors)
        if (m == info.Attacker && !info.Expired)
          return true;

      return false;
    }

    private bool IsAggressed(Mobile m)
    {
      foreach (AggressorInfo info in Caster.Aggressed)
        if (m == info.Defender && !info.Expired)
          return true;

      return false;
    }

    private static bool IsInnocentTo(Mobile from, Mobile to) => Notoriety.Compute(from, to) == Notoriety.Innocent;

    private static bool IsAllyTo(Mobile from, Mobile to) => Notoriety.Compute(from, to) == Notoriety.Ally;
  }
}
