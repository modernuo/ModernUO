using Server.Items;

namespace Server.Spells.Ninjitsu
{
    public class FocusAttack : NinjaMove
    {
        public override int BaseMana => Core.ML ? 10 : 20;
        public override double RequiredSkill => Core.ML ? 30.0 : 60;

        // You prepare to focus all of your abilities into your next strike.
        public override TextDefinition AbilityMessage { get; } = 1063095;

        public override bool Validate(Mobile from)
        {
            var twoHanded = from.FindItemOnLayer(Layer.TwoHanded);
            if (twoHanded is BaseShield)
            {
                from.SendLocalizedMessage(1063096); // You cannot use this ability while holding a shield.
                return false;
            }

            var meleeWeapon =
                twoHanded is BaseWeapon and not BaseRanged ||
                from.FindItemOnLayer(Layer.OneHanded) is BaseWeapon and not BaseRanged;

            if (meleeWeapon)
            {
                return base.Validate(from);
            }

            from.SendLocalizedMessage(1063097); // You must be wielding a melee weapon without a shield to use this ability.
            return false;
        }

        public override double GetDamageScalar(Mobile attacker, Mobile defender)
        {
            var ninjitsu = attacker.Skills.Ninjitsu.Value;

            return 1.0 + ninjitsu * ninjitsu / 43636;
        }

        public override double GetPropertyBonus(Mobile attacker)
        {
            var ninjitsu = attacker.Skills.Ninjitsu.Value;

            var bonus = ninjitsu * ninjitsu / 43636;

            return 1.0 + (bonus * 3 + 0.01);
        }

        public override bool OnBeforeDamage(Mobile attacker, Mobile defender) =>
            Validate(attacker) && CheckMana(attacker, true);

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            ClearCurrentMove(attacker);

            attacker.SendLocalizedMessage(1063098); // You focus all of your abilities and strike with deadly force!
            attacker.PlaySound(0x510);

            CheckGain(attacker);
        }
    }
}
