using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Spellweaving
{
  public class ImmolatingWeaponSpell : ArcanistSpell
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Immolating Weapon", "Thalshara",
      -1);

    private static readonly Dictionary<BaseWeapon, ImmolatingWeaponEntry> m_WeaponDamageTable =
      new Dictionary<BaseWeapon, ImmolatingWeaponEntry>();

    public ImmolatingWeaponSpell(Mobile caster, Item scroll = null)
      : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

    public override double RequiredSkill => 10.0;
    public override int RequiredMana => 32;

    public override bool CheckCast()
    {
      if (!(Caster.Weapon is BaseWeapon weapon) || weapon is Fists || weapon is BaseRanged)
      {
        Caster.SendLocalizedMessage(1060179); // You must be wielding a weapon to use this ability!
        return false;
      }

      return base.CheckCast();
    }

    public override void OnCast()
    {
      if (!(Caster.Weapon is BaseWeapon weapon) || weapon is Fists || weapon is BaseRanged)
      {
        Caster.SendLocalizedMessage(1060179); // You must be wielding a weapon to use this ability!
      }
      else if (CheckSequence())
      {
        Caster.PlaySound(0x5CA);
        Caster.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);

        if (!IsImmolating(weapon)) // On OSI, the effect is not re-applied
        {
          double skill = Caster.Skills.Spellweaving.Value;

          int duration = 10 + (int)(skill / 24) + FocusLevel;
          int damage = 5 + (int)(skill / 24) + FocusLevel;

          Timer stopTimer = Timer.DelayCall(TimeSpan.FromSeconds(duration), StopImmolating, weapon);

          m_WeaponDamageTable[weapon] = new ImmolatingWeaponEntry(damage, stopTimer, Caster);
          weapon.InvalidateProperties();
        }
      }

      FinishSequence();
    }

    public static bool IsImmolating(BaseWeapon weapon) => m_WeaponDamageTable.ContainsKey(weapon);

    public static int GetImmolatingDamage(BaseWeapon weapon) => m_WeaponDamageTable.TryGetValue(weapon, out ImmolatingWeaponEntry entry) ? entry.m_Damage : 0;

    public static void DoEffect(BaseWeapon weapon, Mobile target)
    {
      Timer.DelayCall(TimeSpan.FromSeconds(0.25), FinishEffect, new DelayedEffectEntry(weapon, target));
    }

    private static void FinishEffect(DelayedEffectEntry effect)
    {
      if (m_WeaponDamageTable.TryGetValue(effect.m_Weapon, out ImmolatingWeaponEntry entry))
        AOS.Damage(effect.m_Target, entry.m_Caster, entry.m_Damage, 0, 100, 0, 0, 0);
    }

    public static void StopImmolating(BaseWeapon weapon)
    {
      if (!m_WeaponDamageTable.TryGetValue(weapon, out ImmolatingWeaponEntry entry))
        return;

      entry.m_Caster?.PlaySound(0x27);
      entry.m_Timer.Stop();
      m_WeaponDamageTable.Remove(weapon);

      weapon.InvalidateProperties();
    }

    private class ImmolatingWeaponEntry
    {
      public readonly Mobile m_Caster;
      public readonly int m_Damage;
      public readonly Timer m_Timer;

      public ImmolatingWeaponEntry(int damage, Timer stopTimer, Mobile caster)
      {
        m_Damage = damage;
        m_Timer = stopTimer;
        m_Caster = caster;
      }
    }

    private class DelayedEffectEntry
    {
      public readonly Mobile m_Target;
      public readonly BaseWeapon m_Weapon;

      public DelayedEffectEntry(BaseWeapon weapon, Mobile target)
      {
        m_Weapon = weapon;
        m_Target = target;
      }
    }
  }
}
