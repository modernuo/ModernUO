using System;

namespace Server.Items
{
    /// <summary>
    ///     This attack allows you to disrobe your foe.
    /// </summary>
    public class Disrobe : WeaponAbility
    {
        public static readonly TimeSpan BlockEquipDuration = TimeSpan.FromSeconds(5.0);

        public override int BaseMana => 20; // Not Sure what amount of mana a creature uses.

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);
            var toDisrobe = defender.FindItemOnLayer(Layer.InnerTorso);

            if (toDisrobe?.Movable == false)
            {
                toDisrobe = defender.FindItemOnLayer(Layer.OuterTorso);
            }

            var pack = defender.Backpack;

            if (pack == null || toDisrobe?.Movable == false)
            {
                attacker.SendLocalizedMessage(1004001); // You cannot disarm your opponent.
            }
            else
            {
                // attacker.SendLocalizedMessage( 1060092 ); // You disarm their weapon!
                defender.SendLocalizedMessage(1062002); // You can no longer wear your ~1_ARMOR~

                defender.PlaySound(0x3B9);
                // defender.FixedParticles( 0x37BE, 232, 25, 9948, EffectLayer.InnerTorso );

                pack.DropItem(toDisrobe);

                BaseWeapon.BlockEquip(defender, BlockEquipDuration);
            }
        }
    }
}
