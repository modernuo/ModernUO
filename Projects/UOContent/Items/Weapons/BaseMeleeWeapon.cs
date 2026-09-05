using Server.Spells.First;
using Server.Spells.Spellweaving;

namespace Server.Items
{
    public abstract class BaseMeleeWeapon : BaseWeapon
    {
        public BaseMeleeWeapon(int itemID) : base(itemID)
        {
        }

        public BaseMeleeWeapon(Serial serial) : base(serial)
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

            ReactiveArmorSpell.HandleMeleeHit(attacker, defender, ref damage);

            return damage;
        }
    }
}
