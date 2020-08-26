using System;
using System.Collections.Generic;
using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Seventh;

namespace Server.Spells.Fifth
{
  public class IncognitoSpell : MagerySpell
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Incognito", "Kal In Ex",
      206,
      9002,
      Reagent.Bloodmoss,
      Reagent.Garlic,
      Reagent.Nightshade);

    private static readonly Dictionary<Mobile, InternalTimer> m_Timers = new Dictionary<Mobile, InternalTimer>();

    public IncognitoSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fifth;

    public override bool CheckCast()
    {
      if (Sigil.ExistsOn(Caster))
      {
        Caster.SendLocalizedMessage(1010445); // You cannot incognito if you have a sigil
        return false;
      }

      if (!Caster.CanBeginAction<IncognitoSpell>())
      {
        Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
        return false;
      }

      if (Caster.BodyMod == 183 || Caster.BodyMod == 184)
      {
        Caster.SendLocalizedMessage(1042402); // You cannot use incognito while wearing body paint
        return false;
      }

      return true;
    }

    public override void OnCast()
    {
      if (Sigil.ExistsOn(Caster))
      {
        Caster.SendLocalizedMessage(1010445); // You cannot incognito if you have a sigil
      }
      else if (!Caster.CanBeginAction<IncognitoSpell>())
      {
        Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
      }
      else if (Caster.BodyMod == 183 || Caster.BodyMod == 184)
      {
        Caster.SendLocalizedMessage(1042402); // You cannot use incognito while wearing body paint
      }
      else if (DisguiseTimers.IsDisguised(Caster))
      {
        Caster.SendLocalizedMessage(1061631); // You can't do that while disguised.
      }
      else if (!Caster.CanBeginAction<PolymorphSpell>() || Caster.IsBodyMod)
      {
        DoFizzle();
      }
      else if (CheckSequence())
      {
        if (Caster.BeginAction<IncognitoSpell>())
        {
          DisguiseTimers.StopTimer(Caster);

          Caster.HueMod = Caster.Race.RandomSkinHue();
          Caster.NameMod = Caster.Female ? NameList.RandomName("female") : NameList.RandomName("male");

          PlayerMobile pm = Caster as PlayerMobile;

          if (pm?.Race != null)
          {
            pm.SetHairMods(pm.Race.RandomHair(pm.Female), pm.Race.RandomFacialHair(pm.Female));
            pm.HairHue = pm.Race.RandomHairHue();
            pm.FacialHairHue = pm.Race.RandomHairHue();
          }

          Caster.FixedParticles(0x373A, 10, 15, 5036, EffectLayer.Head);
          Caster.PlaySound(0x3BD);

          BaseArmor.ValidateMobile(Caster);
          BaseClothing.ValidateMobile(Caster);

          StopTimer(Caster);

          int timeVal = 6 * Caster.Skills.Magery.Fixed / 50 + 1;

          if (timeVal > 144)
            timeVal = 144;

          TimeSpan length = TimeSpan.FromSeconds(timeVal);

          InternalTimer t = new InternalTimer(Caster, length);
          m_Timers[Caster] = t;

          t.Start();

          BuffInfo.AddBuff(Caster, new BuffInfo(BuffIcon.Incognito, 1075819, length, Caster));
        }
        else
        {
          Caster.SendLocalizedMessage(1079022); // You're already incognitoed!
        }
      }

      FinishSequence();
    }

    public static void StopTimer(Mobile m)
    {
      if (!m_Timers.TryGetValue(m, out InternalTimer t))
        return;

      t.Stop();
      m_Timers.Remove(m);
      BuffInfo.RemoveBuff(m, BuffIcon.Incognito);
    }

    private class InternalTimer : Timer
    {
      private readonly Mobile m_Owner;

      public InternalTimer(Mobile owner, TimeSpan length) : base(length)
      {
        m_Owner = owner;

        /*
        int val = ((6 * owner.Skills.Magery.Fixed) / 50) + 1;

        if (val > 144)
          val = 144;

        Delay = TimeSpan.FromSeconds( val );
         * */
        Priority = TimerPriority.OneSecond;
      }

      protected override void OnTick()
      {
        if (m_Owner.CanBeginAction<IncognitoSpell>())
          return;

        (m_Owner as PlayerMobile)?.SetHairMods(-1, -1);

        m_Owner.BodyMod = 0;
        m_Owner.HueMod = -1;
        m_Owner.NameMod = null;
        m_Owner.EndAction<IncognitoSpell>();

        BaseArmor.ValidateMobile(m_Owner);
        BaseClothing.ValidateMobile(m_Owner);
      }
    }
  }
}
