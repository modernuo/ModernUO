using System;
using System.Linq;
using Server.Spells;

namespace Server.Items
{
    /// <summary>
    ///     A godsend to a warrior surrounded, the Whirlwind Attack allows the fighter to strike at all nearby targets in one mighty
    ///     spinning swing.
    /// </summary>
    public class WhirlwindAttack : WeaponAbility
    {
        public override int BaseMana => 15;

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            var map = attacker.Map;

            if (map == null)
            {
                return;
            }

            if (!(attacker.Weapon is BaseWeapon weapon))
            {
                return;
            }

            attacker.FixedEffect(0x3728, 10, 15);
            attacker.PlaySound(0x2A1);

            var targets = attacker.GetMobilesInRange(1)
                .Where(
                    m =>
                        m?.Deleted == false && m != defender && m != attacker &&
                        SpellHelper.ValidIndirectTarget(attacker, m) &&
                        m.Map == attacker.Map && m.Alive && attacker.CanSee(m) && attacker.CanBeHarmful(m) &&
                        attacker.InRange(m, weapon.MaxRange) && attacker.InLOS(m)
                )
                .ToList();

            if (targets.Count <= 0)
            {
                return;
            }

            var bushido = attacker.Skills.Bushido.Value;
            var damageBonus = 1.0 + Math.Pow(targets.Count * bushido / 60, 2) / 100;

            if (damageBonus > 2.0)
            {
                damageBonus = 2.0;
            }

            attacker.RevealingAction();

            for (var i = 0; i < targets.Count; ++i)
            {
                var m = targets[i];

                attacker.SendLocalizedMessage(1060161); // The whirling attack strikes a target!
                m.SendLocalizedMessage(1060162);        // You are struck by the whirling attack and take damage!

                weapon.OnHit(attacker, m, damageBonus);
            }
        }
    }
}
