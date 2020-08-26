using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Chivalry
{
  public class ConsecrateWeaponSpell : PaladinSpell
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Consecrate Weapon", "Consecrus Arma",
      -1,
      9002);

    private static readonly Dictionary<BaseWeapon, ExpireTimer> m_Table = new Dictionary<BaseWeapon, ExpireTimer>();

    public ConsecrateWeaponSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.5);

    public override double RequiredSkill => 15.0;
    public override int RequiredMana => 10;
    public override int RequiredTithing => 10;
    public override int MantraNumber => 1060720; // Consecrus Arma
    public override bool BlocksMovement => false;

    public override void OnCast()
    {
      if (!(Caster.Weapon is BaseWeapon weapon) || weapon is Fists)
      {
        Caster.SendLocalizedMessage(501078); // You must be holding a weapon.
      }
      else if (CheckSequence())
      {
        /* Temporarily enchants the weapon the caster is currently wielding.
         * The type of damage the weapon inflicts when hitting a target will
         * be converted to the target's worst Resistance type.
         * Duration of the effect is affected by the caster's Karma and lasts for 3 to 11 seconds.
         */

        int itemID, soundID;

        switch (weapon.Skill)
        {
          case SkillName.Macing:
            itemID = 0xFB4;
            soundID = 0x232;
            break;
          case SkillName.Archery:
            itemID = 0x13B1;
            soundID = 0x145;
            break;
          default:
            itemID = 0xF5F;
            soundID = 0x56;
            break;
        }

        Caster.PlaySound(0x20C);
        Caster.PlaySound(soundID);
        Caster.FixedParticles(0x3779, 1, 30, 9964, 3, 3, EffectLayer.Waist);

        IEntity from = new Entity(Serial.Zero, new Point3D(Caster.X, Caster.Y, Caster.Z), Caster.Map);
        IEntity to = new Entity(Serial.Zero, new Point3D(Caster.X, Caster.Y, Caster.Z + 50), Caster.Map);
        Effects.SendMovingParticles(from, to, itemID, 1, 0, false, false, 33, 3, 9501, 1, 0, EffectLayer.Head,
          0x100);

        double seconds = Math.Clamp(ComputePowerValue(20), 3.0, 11.0);

        TimeSpan duration = TimeSpan.FromSeconds(seconds);

        m_Table.TryGetValue(weapon, out ExpireTimer timer);
        timer?.Stop();

        weapon.Consecrated = true;

        m_Table[weapon] = timer = new ExpireTimer(weapon, duration);

        timer.Start();
      }

      FinishSequence();
    }

    private class ExpireTimer : Timer
    {
      private readonly BaseWeapon m_Weapon;

      public ExpireTimer(BaseWeapon weapon, TimeSpan delay) : base(delay)
      {
        m_Weapon = weapon;
        Priority = TimerPriority.FiftyMS;
      }

      protected override void OnTick()
      {
        m_Weapon.Consecrated = false;
        Effects.PlaySound(m_Weapon.GetWorldLocation(), m_Weapon.Map, 0x1F8);
        m_Table.Remove(m_Weapon);
      }
    }
  }
}
