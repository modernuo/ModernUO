using System;
using System.Collections.Generic;
using Server.Factions;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Fifth;

namespace Server.Spells.Seventh
{
  public class PolymorphSpell : MagerySpell
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Polymorph", "Vas Ylem Rel",
      221,
      9002,
      Reagent.Bloodmoss,
      Reagent.SpidersSilk,
      Reagent.MandrakeRoot);

    private static readonly Dictionary<Mobile, InternalTimer> m_Timers = new Dictionary<Mobile, InternalTimer>();

    private readonly int m_NewBody;

    public PolymorphSpell(Mobile caster, Item scroll, int body = 0) : base(caster, scroll, m_Info) => m_NewBody = body;

    public override SpellCircle Circle => SpellCircle.Seventh;

    public override bool CheckCast()
    {
      /*if (Caster.Mounted)
      {
        Caster.SendLocalizedMessage( 1042561 ); //Please dismount first.
        return false;
      }
      else */
      if (Sigil.ExistsOn(Caster))
      {
        Caster.SendLocalizedMessage(1010521); // You cannot polymorph while you have a Town Sigil
        return false;
      }

      if (TransformationSpellHelper.UnderTransformation(Caster))
      {
        Caster.SendLocalizedMessage(1061633); // You cannot polymorph while in that form.
        return false;
      }

      if (DisguiseTimers.IsDisguised(Caster))
      {
        Caster.SendLocalizedMessage(502167); // You cannot polymorph while disguised.
        return false;
      }

      if (Caster.BodyMod == 183 || Caster.BodyMod == 184)
      {
        Caster.SendLocalizedMessage(1042512); // You cannot polymorph while wearing body paint
        return false;
      }

      if (!Caster.CanBeginAction<PolymorphSpell>())
      {
        if (Core.ML)
          EndPolymorph(Caster);
        else
          Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
        return false;
      }

      if (m_NewBody == 0)
      {
        Gump gump = Core.SE ? (Gump)new NewPolymorphGump(Caster, Scroll) : new PolymorphGump(Caster, Scroll);

        Caster.SendGump(gump);
        return false;
      }

      return true;
    }

    public override void OnCast()
    {
      /*if (Caster.Mounted)
      {
        Caster.SendLocalizedMessage( 1042561 ); //Please dismount first.
      }
      else */
      if (Sigil.ExistsOn(Caster))
      {
        Caster.SendLocalizedMessage(1010521); // You cannot polymorph while you have a Town Sigil
      }
      else if (!Caster.CanBeginAction<PolymorphSpell>())
      {
        if (Core.ML)
          EndPolymorph(Caster);
        else
          Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
      }
      else if (TransformationSpellHelper.UnderTransformation(Caster))
      {
        Caster.SendLocalizedMessage(1061633); // You cannot polymorph while in that form.
      }
      else if (DisguiseTimers.IsDisguised(Caster))
      {
        Caster.SendLocalizedMessage(502167); // You cannot polymorph while disguised.
      }
      else if (Caster.BodyMod == 183 || Caster.BodyMod == 184)
      {
        Caster.SendLocalizedMessage(1042512); // You cannot polymorph while wearing body paint
      }
      else if (!Caster.CanBeginAction<IncognitoSpell>() || Caster.IsBodyMod)
      {
        DoFizzle();
      }
      else if (CheckSequence())
      {
        if (Caster.BeginAction<PolymorphSpell>())
        {
          if (m_NewBody != 0)
          {
            if (!((Body)m_NewBody).IsHuman)
            {
              IMount mt = Caster.Mount;

              if (mt != null)
                mt.Rider = null;
            }

            Caster.BodyMod = m_NewBody;

            if (m_NewBody == 400 || m_NewBody == 401)
              Caster.HueMod = Caster.Race.RandomSkinHue();
            else
              Caster.HueMod = 0;

            BaseArmor.ValidateMobile(Caster);
            BaseClothing.ValidateMobile(Caster);

            if (!Core.ML)
            {
              StopTimer(Caster);

              InternalTimer timer = new InternalTimer(Caster);

              m_Timers[Caster] = timer;

              timer.Start();
            }
          }
        }
        else
        {
          Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
        }
      }

      FinishSequence();
    }

    public static void StopTimer(Mobile m)
    {
      if (!m_Timers.TryGetValue(m, out InternalTimer timer))
        return;

      timer?.Stop();
      m_Timers.Remove(m);
    }

    private static void EndPolymorph(Mobile m)
    {
      if (m.CanBeginAction<PolymorphSpell>())
        return;

      m.BodyMod = 0;
      m.HueMod = -1;
      m.EndAction<PolymorphSpell>();

      BaseArmor.ValidateMobile(m);
      BaseClothing.ValidateMobile(m);
    }

    private class InternalTimer : Timer
    {
      private readonly Mobile m_Owner;

      public InternalTimer(Mobile owner) : base(TimeSpan.FromSeconds(0))
      {
        m_Owner = owner;

        int val = (int)owner.Skills.Magery.Value;

        if (val > 120)
          val = 120;

        Delay = TimeSpan.FromSeconds(val);
        Priority = TimerPriority.OneSecond;
      }

      protected override void OnTick()
      {
        EndPolymorph(m_Owner);
      }
    }
  }
}
