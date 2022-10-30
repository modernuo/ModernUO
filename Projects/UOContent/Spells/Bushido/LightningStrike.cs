using Server.Mobiles;

namespace Server.Spells.Bushido
{
    public class LightningStrike : SamuraiMove
    {
        public override int BaseMana => 5;
        public override double RequiredSkill => 50.0;

        public override TextDefinition AbilityMessage { get; } = 1063167; // You prepare to strike quickly.

        public override bool DelayedContext => true;

        public override bool ValidatesDuringHit => false;

        public override int GetAccuracyBonus(Mobile attacker) => 50;

        public override bool Validate(Mobile from)
        {
            var isValid = base.Validate(from);
            if (isValid && from is PlayerMobile pm)
            {
                pm.ExecutesLightningStrike = BaseMana;
            }

            return isValid;
        }

        public override bool IgnoreArmor(Mobile attacker)
        {
            var bushido = attacker.Skills.Bushido.Value;
            var criticalChance = bushido * bushido / 72000.0;
            return criticalChance >= Utility.RandomDouble();
        }

        public override bool OnBeforeSwing(Mobile attacker, Mobile defender)
        {
            /* no mana drain before actual hit */
            var enoughMana = CheckMana(attacker, false);
            return Validate(attacker);
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            ClearCurrentMove(attacker);
            if (CheckMana(attacker, true))
            {
                attacker.SendLocalizedMessage(1063168); // You attack with lightning precision!
                defender.SendLocalizedMessage(1063169); // Your opponent's quick strike causes extra damage!
                defender.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Waist);
                defender.PlaySound(0x51D);
                CheckGain(attacker);
                SetContext(attacker);
            }
        }

        public override void OnClearMove(Mobile attacker)
        {
            if (attacker is PlayerMobile pm)
            {
                pm.ExecutesLightningStrike = 0;
            }
        }
    }
}
