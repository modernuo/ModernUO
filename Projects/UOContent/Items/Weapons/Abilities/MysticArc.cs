using System;
using Server.Collections;

namespace Server.Items;

// The thrown projectile will arc to a second target after hitting the primary target.
// Chaos energy will burst from the projectile at each target. This will only hit targets that are in combat with the user.
public class MysticArc : WeaponAbility
{
    private const int Damage = 15;

    public override int BaseMana => 20;

    public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
    {
        if (!CheckMana(attacker, true) && defender != null)
        {
            return;
        }

        var weapon = attacker.Weapon as BaseThrown;

        if (weapon == null)
        {
            return;
        }

        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (var m in attacker.GetMobilesInRange(weapon.MaxRange))
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

        var target = queue.Count > 0 ? queue.PeekRandom() : null;

        AOS.Damage(defender, attacker, Damage, 0, 0, 0, 0, 0, 100);

        if (target != null)
        {
            defender?.MovingEffect(target, weapon.ItemID, 18, 1, false, false);

            Timer.DelayCall(TimeSpan.FromSeconds(0.3), () => ThrowAgain(attacker, target));
        }

        ClearCurrentAbility(attacker);
    }

    private static void ThrowAgain(Mobile attacker, Mobile target)
    {
        if (attacker.Weapon is not BaseThrown weapon)
        {
            return;
        }

        if (GetCurrentAbility(attacker) is MysticArc)
        {
            ClearCurrentAbility(attacker);
        }

        if (weapon.CheckHit(attacker, target))
        {
            weapon.OnHit(attacker, target, 0.0);
            AOS.Damage(target, attacker, Damage, 0, 0, 0, 0, 100);
        }
    }
}
