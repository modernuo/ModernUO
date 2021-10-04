namespace Server.Items
{
    public class SerpentArrow : WeaponAbility
    {
        public override int BaseMana => 25;

        // Serpent arrow is used only as secondary ability on "Elven composite longbow" so needed skill is always 60, but for sure we will use GetRequiredSecondarySkill
        public override bool RequiresSecondarySkill(Mobile from) => true;
        public override double GetRequiredSecondarySkill(Mobile from) => 60;
        public override SkillName GetSecondarySkillName(Mobile from) => SkillName.Poisoning;

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            defender.SendLocalizedMessage(1112369); // You have been poisoned by a lethal arrow!

            int level;

            if (attacker.InRange(defender, 2))
            {
                level = attacker.Skills.Poisoning.Fixed / 2 switch
                {
                    >= 1000 => 3,
                    > 850   => 2,
                    > 650   => 1,
                    _       => 0
                };
            }
            else
            {
                level = 0;
            }

            defender.ApplyPoison(attacker, Poison.GetPoison(level));

            defender.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
            defender.PlaySound(0x474);
        }
    }
}
