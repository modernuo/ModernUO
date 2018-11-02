using System;
using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Sixth
{
  public class InvisibilitySpell : MagerySpell
  {
    private static SpellInfo m_Info = new SpellInfo(
      "Invisibility", "An Lor Xen",
      206,
      9002,
      Reagent.Bloodmoss,
      Reagent.Nightshade
    );

    private static Dictionary<Mobile, Timer> m_Table = new Dictionary<Mobile, Timer>();

    public InvisibilitySpell(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Sixth;

    public override bool CheckCast()
    {
      if (DuelContext.CheckSuddenDeath(Caster))
      {
        Caster.SendMessage(0x22, "You cannot cast this spell when in sudden death.");
        return false;
      }

      return base.CheckCast();
    }

    public override void OnCast()
    {
      Caster.Target = new InternalTarget(this);
    }

    public void Target(Mobile m)
    {
      if (!Caster.CanSee(m))
      {
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      }
      else if (m is BaseVendor || m is PlayerVendor || m.AccessLevel > Caster.AccessLevel)
      {
        Caster.SendLocalizedMessage(501857); // This spell won't work on that!
      }
      else if (CheckBSequence(m))
      {
        SpellHelper.Turn(Caster, m);

        Effects.SendLocationParticles(
          EffectItem.Create(new Point3D(m.X, m.Y, m.Z + 16), Caster.Map, EffectItem.DefaultDuration), 0x376A, 10,
          15, 5045);
        m.PlaySound(0x3C4);

        m.Hidden = true;
        m.Combatant = null;
        m.Warmode = false;

        RemoveTimer(m);

        TimeSpan duration = TimeSpan.FromSeconds(1.2 * Caster.Skills.Magery.Fixed / 10);

        Timer t = new InternalTimer(m, duration);

        BuffInfo.RemoveBuff(m, BuffIcon.HidingAndOrStealth);
        BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Invisibility, 1075825, duration, m)); //Invisibility/Invisible

        m_Table[m] = t;

        t.Start();
      }

      FinishSequence();
    }

    public static bool HasTimer(Mobile m)
    {
      return m_Table.ContainsKey(m);
    }

    public static void RemoveTimer(Mobile m)
    {
      if (!m_Table.TryGetValue(m, out Timer t))
        return;

      t.Stop();
      m_Table.Remove(m);
    }

    private class InternalTimer : Timer
    {
      private Mobile m_Mobile;

      public InternalTimer(Mobile m, TimeSpan duration) : base(duration)
      {
        Priority = TimerPriority.OneSecond;
        m_Mobile = m;
      }

      protected override void OnTick()
      {
        m_Mobile.RevealingAction();
        RemoveTimer(m_Mobile);
      }
    }

    public class InternalTarget : Target
    {
      private InvisibilitySpell m_Owner;

      public InternalTarget(InvisibilitySpell owner) : base(Core.ML ? 10 : 12, false, TargetFlags.Beneficial)
      {
        m_Owner = owner;
      }

      protected override void OnTarget(Mobile from, object o)
      {
        if (o is Mobile mobile)
          m_Owner.Target(mobile);
      }

      protected override void OnTargetFinish(Mobile from)
      {
        m_Owner.FinishSequence();
      }
    }
  }
}
