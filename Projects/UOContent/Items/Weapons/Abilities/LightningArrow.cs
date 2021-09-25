using Server.Spells;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class LightningArrow : WeaponAbility
    {
        public override int BaseMana => 20;

        //TODO - add ConsumeAmmo on weaponabilities
       // public override bool ConsumeAmmo => false;

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            Map map = attacker.Map;

            if (map == null)
            {
                return;
            }

            BaseWeapon weapon = attacker.Weapon as BaseWeapon;

            if (weapon == null)
            {
                return;
            }

            if (!CheckMana(attacker, true))
            {
                return;
            }

            List<Mobile> targets = new List<Mobile>();
            IPooledEnumerable eable = defender.GetMobilesInRange(5);

            foreach (Mobile m in eable)
            {
                if (m != defender && m != attacker && SpellHelper.ValidIndirectTarget(attacker, m))
                {
                    if (m == null || m.Deleted || m.Map != attacker.Map || !m.Alive || !attacker.CanSee(m) || !attacker.CanBeHarmful(m))
                        continue;

                    if (!attacker.InRange(m, weapon.MaxRange) || !attacker.InLOS(m))
                        continue;

                    targets.Add(m);
                }
            }

            eable.Free();
            defender.BoltEffect(0);

            var mobilesLeft = Math.Min(targets.Count, 2);
            while (mobilesLeft-- > 0)
            {
                var index = Utility.Random(targets.Count);
                var m = targets[index];
                targets.RemoveAt(index);

                m.BoltEffect(0);
                AOS.Damage(m, attacker, Utility.RandomMinMax(29, 40), 0, 0, 0, 0, 100);
            }
        }
    }
}
