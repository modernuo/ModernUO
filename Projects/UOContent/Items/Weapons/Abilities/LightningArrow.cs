using Server.Spells;
using System;
using Server.Collections;

namespace Server.Items
{
    public class LightningArrow : WeaponAbility
    {
        public override int BaseMana => 20;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            Map map = attacker.Map;

            if (map == null || attacker.Weapon is not BaseWeapon weapon || !CheckMana(attacker, true))
            {
                return;
            }

            using var list = PooledRefList<Mobile>.Create();
            foreach (Mobile m in worldLocation.Map.GetMobilesInRange(5))
            {
                if (m != defender && m != attacker && SpellHelper.ValidIndirectTarget(attacker, m) && m?.Deleted == false &&
                    m.Map == attacker.Map && m.Alive && attacker.CanSee(m) && attacker.CanBeHarmful(m) &&
                    attacker.InRange(m, weapon.MaxRange) && attacker.InLOS(m))
                {
                    list.Add(m);
                }
            }

            if (defender?.Deleted == false && defender.Map != null)
            {
                defender.BoltEffect(0);
                AOS.Damage(defender, attacker, Utility.RandomMinMax(29, 40), 0, 0, 0, 0, 100);
            }

            var count = Math.Min(list.Count, 2);
            if (count > 0)
            {
                list.Shuffle();

                for (var i = 0; i < count; i++)
                {
                    var m = list[i];
                    m.BoltEffect(0);
                    AOS.Damage(m, attacker, Utility.RandomMinMax(29, 40), 0, 0, 0, 0, 100);
                }
            }
        }
    }
}
