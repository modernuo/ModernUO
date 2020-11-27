using System.Linq;

namespace Server.Spells.Bushido
{
    public class MomentumStrike : SamuraiMove
    {
        public override int BaseMana => 10;
        public override double RequiredSkill => 70.0;

        public override TextDefinition AbilityMessage =>
            new(1070757); // You prepare to strike two enemies with one blow.

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, false))
            {
                return;
            }

            ClearCurrentMove(attacker);

            var weapon = attacker.Weapon;

            var targets = attacker.GetMobilesInRange(weapon.MaxRange)
                .Where(m => m != defender)
                .Where(m => m.Combatant == attacker)
                .ToList();

            if (targets.Count <= 0)
            {
                attacker.SendLocalizedMessage(1063123); // There are no valid targets to attack!
                return;
            }

            if (!CheckMana(attacker, true))
            {
                return;
            }

            var target = targets.RandomElement();

            var damageBonus = attacker.Skills.Bushido.Value / 100.0;

            if (!defender.Alive)
            {
                damageBonus *= 1.5;
            }

            attacker.SendLocalizedMessage(1063171); // You transfer the momentum of your weapon into another enemy!
            target.SendLocalizedMessage(1063172);   // You were hit by the momentum of a Samurai's weapon!

            target.FixedParticles(0x37B9, 1, 4, 0x251D, 0, 0, EffectLayer.Waist);

            attacker.PlaySound(0x510);

            weapon.OnSwing(attacker, target, damageBonus);

            CheckGain(attacker);
        }

        public override void CheckGain(Mobile m)
        {
            m.CheckSkill(MoveSkill, RequiredSkill, 120.0);
        }
    }
}
