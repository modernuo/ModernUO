using System;

namespace Server.Items
{
    /// <summary>
    ///     This special move represents a significant change to the use of poisons in Age of Shadows.
    ///     Now, only certain weapon types, those that have Infectious Strike as an available special move will be able to be
    ///     poisoned.
    ///     Targets will no longer be poisoned at random when hit by poisoned weapons.
    ///     Instead, the wielder must use this ability to deliver the venom.
    ///     While no skill in Poisoning is directly required to use this ability, being knowledgeable in the application and use of
    ///     toxins
    ///     will allow a character to use Infectious Strike at reduced mana cost and with a chance to inflict more deadly poison on
    ///     his victim.
    ///     With this change, weapons will no longer be corroded by poison.
    ///     Level 5 poison will be possible when using this special move.
    /// </summary>
    public class InfectiousStrike : WeaponAbility
    {
        public override int BaseMana => 15;

        public override bool RequiresTactics(Mobile from) => false;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            if (attacker.Weapon is not BaseWeapon weapon)
            {
                return;
            }

            var p = weapon.Poison;

            if (p == null || weapon.PoisonCharges <= 0)
            {
                // Your weapon must have a dose of poison to perform an infectious strike!
                attacker.SendLocalizedMessage(1061141);
                return;
            }

            --weapon.PoisonCharges;

            // Infectious strike special move now uses poisoning skill to help determine potency
            var maxLevel = Math.Max((int)(attacker.Skills.Poisoning.Value / 20), 0);
            if (p.Level > maxLevel)
            {
                p = Poison.GetPoison(maxLevel);
            }

            if (attacker.Skills.Poisoning.Value / 100.0 > Utility.RandomDouble())
            {
                var level = p.Level + 1;
                var newPoison = Poison.GetPoison(level);

                if (newPoison != null)
                {
                    p = newPoison;

                    attacker.SendLocalizedMessage(1060080); // Your precise strike has increased the level of the poison by 1
                    defender.SendLocalizedMessage(1060081); // The poison seems extra effective!
                }
            }

            defender.PlaySound(0xDD);
            defender.FixedParticles(0x3728, 244, 25, 9941, 1266, 0, EffectLayer.Waist);

            if (defender.ApplyPoison(attacker, p) != ApplyPoisonResult.Immune)
            {
                attacker.SendLocalizedMessage(1008096, true, defender.Name);  // You have poisoned your target :
                defender.SendLocalizedMessage(1008097, false, attacker.Name); //  : poisoned you!
            }
        }
    }
}
