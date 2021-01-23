namespace Server.Items
{
    /// <summary>
    ///     Strike your opponent with great force, partially bypassing their armor and inflicting greater damage. Requires either
    ///     Bushido or Ninjitsu skill
    /// </summary>
    public class ArmorPierce : WeaponAbility
    {
        public override int BaseMana => 30;
        public override double DamageScalar => 1.5;

        public override bool RequiresSE => true;

        public override bool CheckSkills(Mobile from)
        {
            if (GetSkill(from, SkillName.Ninjitsu) < 50.0 && GetSkill(from, SkillName.Bushido) < 50.0)
            {
                // You need ~1_SKILL_REQUIREMENT~ Bushido or Ninjitsu skill to perform that attack!
                from.SendLocalizedMessage(1063347, "50");
                return false;
            }

            return base.CheckSkills(from);
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1063350); // You pierce your opponent's armor!
            defender.SendLocalizedMessage(1063351); // Your attacker pierced your armor!

            defender.FixedParticles(0x3728, 1, 26, 0x26D6, 0, 0, EffectLayer.Waist);
        }
    }
}
