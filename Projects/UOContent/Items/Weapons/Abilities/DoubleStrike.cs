namespace Server.Items
{
    /// <summary>
    ///     The highly skilled warrior can use this special attack to make two quick swings in succession.
    ///     Landing both blows would be devastating!
    /// </summary>
    public class DoubleStrike : WeaponAbility
    {
        public override int BaseMana => 30;
        public override double DamageScalar => 0.9;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1060084); // You attack with lightning speed!
            defender.SendLocalizedMessage(1060085); // Your attacker strikes with lightning speed!

            defender.PlaySound(0x3BB);
            defender.FixedEffect(0x37B9, 244, 25);

            // Swing again:

            // If no combatant, wrong map, one of us is a ghost, or cannot see, or deleted, then stop combat
            if (defender.Deleted || attacker.Deleted || defender.Map != attacker.Map ||
                !defender.Alive || !attacker.Alive || !attacker.CanSee(defender))
            {
                attacker.Combatant = null;
                return;
            }

            var weapon = attacker.Weapon;

            if (!(weapon != null && attacker.InRange(defender, weapon.MaxRange) && attacker.InLOS(defender)))
            {
                return;
            }

            BaseWeapon.InDoubleStrike = true;
            attacker.RevealingAction();
            attacker.NextCombatTime = Core.TickCount + (int)weapon.OnSwing(attacker, defender).TotalMilliseconds;
            BaseWeapon.InDoubleStrike = false;
        }
    }
}
