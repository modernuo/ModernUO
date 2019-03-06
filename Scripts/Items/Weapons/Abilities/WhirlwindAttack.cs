using System;
using System.Collections.Generic;
using Server.Spells;

namespace Server.Items
{
  /// <summary>
  ///   A godsend to a warrior surrounded, the Whirlwind Attack allows the fighter to strike at all nearby targets in one mighty
  ///   spinning swing.
  /// </summary>
  public class WhirlwindAttack : WeaponAbility
  {
    public override int BaseMana => 15;

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
      if (!Validate(attacker))
        return;

      ClearCurrentAbility(attacker);

      Map map = attacker.Map;

      if (map == null)
        return;

      if (!(attacker.Weapon is BaseWeapon weapon))
        return;

      if (!CheckMana(attacker, true))
        return;

      attacker.FixedEffect(0x3728, 10, 15);
      attacker.PlaySound(0x2A1);

      List<Mobile> list = new List<Mobile>();

      foreach (Mobile m in attacker.GetMobilesInRange(1))
        list.Add(m);

      List<Mobile> targets = new List<Mobile>();

      for (int i = 0; i < list.Count; ++i)
      {
        Mobile m = list[i];

        if (m?.Deleted == false && m != defender && m != attacker && SpellHelper.ValidIndirectTarget(attacker, m) &&
            m.Map == attacker.Map && m.Alive && attacker.CanSee(m) && attacker.CanBeHarmful(m) &&
            attacker.InRange(m, weapon.MaxRange) && attacker.InLOS(m))
          targets.Add(m);
      }

      if (targets.Count > 0)
      {
        double bushido = attacker.Skills.Bushido.Value;
        double damageBonus = 1.0 + Math.Pow(targets.Count * bushido / 60, 2) / 100;

        if (damageBonus > 2.0)
          damageBonus = 2.0;

        attacker.RevealingAction();

        for (int i = 0; i < targets.Count; ++i)
        {
          Mobile m = targets[i];

          attacker.SendLocalizedMessage(1060161); // The whirling attack strikes a target!
          m.SendLocalizedMessage(1060162); // You are struck by the whirling attack and take damage!

          weapon.OnHit(attacker, m, damageBonus);
        }
      }
    }
  }
}
