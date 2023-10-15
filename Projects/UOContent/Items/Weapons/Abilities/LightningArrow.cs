using Server.Spells;
using System;
using Server.Collections;

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

            if (map == null || attacker.Weapon is not BaseWeapon weapon || !CheckMana(attacker, true))
            {
                return;
            }

            using var list = PooledRefList<Mobile>.Create();
            foreach (Mobile m in defender.GetMobilesInRange(5))
            {
                if (m != defender && m != attacker && SpellHelper.ValidIndirectTarget(attacker, m) && m?.Deleted == false &&
                    m.Map == attacker.Map && m.Alive && attacker.CanSee(m) && attacker.CanBeHarmful(m) &&
                    attacker.InRange(m, weapon.MaxRange) && attacker.InLOS(m))
                {
                    list.Add(m);
                }
            }

            defender.BoltEffect(0);

            var count = Math.Min(list.Count, 2);
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
