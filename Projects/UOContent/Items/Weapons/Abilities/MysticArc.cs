using System;
using Server.Collections;

namespace Server.Items;

// The thrown projectile will arc to a second target after hitting the primary target.
// Chaos energy will burst from the projectile at each target. This will only hit targets that are in combat with the user.
public class MysticArc : WeaponAbility
{
    private readonly int Damage = 15;
    private Mobile _target;
    private Mobile _mobile;

    public override int BaseMana => 20;

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
        if (!CheckMana(attacker, true) && defender != null)
        {
            return;
        }

        BaseThrown weapon = attacker.Weapon as BaseThrown;

        if (weapon == null)
        {
            return;
        }

        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (Mobile m in attacker.GetMobilesInRange(weapon.MaxRange))
        {
            if (m == defender)
            {
                continue;
            }

            if (m.Combatant != attacker)
            {
                continue;
            }

            queue.Enqueue(m);
        }

        if (queue.Count > 0)
        {
            _target = queue.PeekRandom();
        }

        AOS.Damage(defender, attacker, Damage, 0, 0, 0, 0, 0, 100);

        if (_target != null)
        {
            defender?.MovingEffect(_target, weapon.ItemID, 18, 1, false, false);

            Timer.DelayCall(TimeSpan.FromSeconds(0.3), ThrowAgain);
            _mobile = attacker;
        }

        ClearCurrentAbility(attacker);
    }

    public void ThrowAgain()
    {
        if (_target == null || _mobile == null)
        {
            return;
        }

        if (_mobile.Weapon is not BaseThrown weapon)
        {
            return;
        }

        if (GetCurrentAbility(_mobile) is MysticArc)
        {
            ClearCurrentAbility(_mobile);
        }

        if (weapon.CheckHit(_mobile, _target))
        {
            weapon.OnHit(_mobile, _target, 0.0);
            AOS.Damage(_target, _mobile, Damage, 0, 0, 0, 0, 100);
        }
    }
}
