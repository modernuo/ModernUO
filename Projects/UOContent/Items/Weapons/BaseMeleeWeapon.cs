using Server.Spells.Spellweaving;

namespace Server.Items
{
    [Serializable(0, false)]
    public abstract partial class BaseMeleeWeapon : BaseWeapon
    {
        public BaseMeleeWeapon(int itemID) : base(itemID)
        {
        }

        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage)
        {
            damage = base.AbsorbDamage(attacker, defender, damage);

            AttuneWeaponSpell.TryAbsorb(defender, ref damage);

            if (Core.AOS)
            {
                return damage;
            }

            var absorb = defender.MeleeDamageAbsorb;

            if (absorb > 0)
            {
                if (absorb > damage)
                {
                    var react = damage / 5;

                    if (react <= 0)
                    {
                        react = 1;
                    }

                    defender.MeleeDamageAbsorb -= damage;
                    damage = 0;

                    attacker.Damage(react, defender);

                    attacker.PlaySound(0x1F1);
                    attacker.FixedEffect(0x374A, 10, 16);
                }
                else
                {
                    defender.MeleeDamageAbsorb = 0;
                    defender.SendLocalizedMessage(1005556); // Your reactive armor spell has been nullified.
                    DefensiveSpell.Nullify(defender);
                }
            }

            return damage;
        }
    }
}
