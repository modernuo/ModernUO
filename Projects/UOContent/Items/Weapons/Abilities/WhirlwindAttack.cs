using System;
using Server.Collections;
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

            if (attacker.Weapon is not BaseWeapon weapon)
            {
                return;
            }

            attacker.FixedEffect(0x3728, 10, 15);
            attacker.PlaySound(0x2A1);

            var eable = attacker.GetMobilesInRange(1);
            using var queue = PooledRefQueue<Mobile>.Create();

            foreach (var m in eable)
            {
                if (m?.Deleted == false && m != defender && m != attacker &&
                    m.Map == attacker.Map && m.Alive &&
                    SpellHelper.ValidIndirectTarget(attacker, m) &&
                    attacker.CanSee(m) && attacker.CanBeHarmful(m) &&
                    attacker.InRange(m, weapon.MaxRange) && attacker.InLOS(m))
                {
                    queue.Enqueue(m);
                }
            }

            if (queue.Count <= 0)
            {
                return;
            }

            var bushido = attacker.Skills.Bushido.Value;
            var damageBonus = 1.0 + Math.Pow(queue.Count * bushido / 60, 2) / 100;

            if (damageBonus > 2.0)
            {
                damageBonus = 2.0;
            }

            attacker.RevealingAction();

            while (queue.Count > 0)
            {
                var m = queue.Dequeue();
                attacker.SendLocalizedMessage(1060161); // The whirling attack strikes a target!
                m.SendLocalizedMessage(1060162);        // You are struck by the whirling attack and take damage!

                weapon.OnHit(attacker, m, damageBonus);
            }
        }
    }
}
