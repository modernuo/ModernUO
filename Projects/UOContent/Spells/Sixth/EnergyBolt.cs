using Server.Targeting;

namespace Server.Spells.Sixth
{
    public class EnergyBoltSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Energy Bolt",
            "Corp Por",
            230,
            9022,
            Reagent.BlackPearl,
            Reagent.Nightshade
        );

        public EnergyBoltSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Sixth;

        public override bool DelayedDamage => true;

        public void Target(Mobile defender)
        {
            if (CheckHSequence(defender))
            {
                var attacker = Caster;

                SpellHelper.Turn(Caster, defender);

                SpellHelper.CheckReflect((int)Circle, ref attacker, ref defender);

                double damage;

                if (Core.AOS)
                {
                    damage = GetNewAosDamage(40, 1, 5, defender);
                }
                else
                {
                    damage = Utility.Random(24, 18);

                    if (CheckResisted(defender))
                    {
                        damage *= 0.75;

                        defender.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    // Scale damage based on evalint and resist
                    damage *= GetDamageScalar(defender);
                }

                int deltaX = defender.X - attacker.X;
                int deltaY = defender.Y - attacker.Y;
            
                int xOffset = 0, yOffset = 0;
                int zOffset = attacker.Mount != null ? 0 : 0;
            
                if (deltaX == 0 && deltaY < 0)
                {
                    xOffset = 0;
                    yOffset = 0;
                    zOffset = attacker.Mount != null ? 12 : 8;
                }
                else if (deltaX > 0 && deltaY < 0)
                {
                    xOffset = 0;
                    yOffset = 0;
                    zOffset = attacker.Mount != null ? 12 : 6;
                }
                else if (deltaX > 0 && deltaY == 0)
                {

                    xOffset = 0;
                    yOffset = 0;
                    zOffset = attacker.Mount != null ? 9 : 5;
                }
                else if (deltaX > 0 && deltaY > 0)
                {
                    xOffset = 0;
                    yOffset = 1;
                    zOffset = attacker.Mount != null ? 16 : 8;
                }
                else if (deltaX == 0 && deltaY > 0)
                {
                    xOffset = 0;
                    yOffset = 1;
                    zOffset = attacker.Mount != null ? 25 : 17;
                
                }
                else if (deltaX < 0 && deltaY > 0)
                {
                    xOffset = -1;
                    yOffset = 1;
                    zOffset = attacker.Mount != null ? 16 : 8;
                }
                else if (deltaX < 0 && deltaY == 0)

                {
                    xOffset = -1;
                    yOffset = 0;
                    zOffset = attacker.Mount != null ? 15 : 9;
                }
                else if (deltaX < 0 && deltaY < 0)
                {
                    xOffset = 0;
                    yOffset = 0;
                    zOffset = attacker.Mount != null ? 16 : 8;
                }
            
                Point3D from = new(attacker.X + xOffset, attacker.Y + yOffset, attacker.Z + zOffset);
                Point3D to = new(defender.X + xOffset, defender.Y + yOffset, defender.Z + zOffset);
            
                Effects.SendMovingEffect(new Entity(Serial.Zero, from, attacker.Map),
                    new Entity(Serial.Zero, to, defender.Map), 0x379F, 7, 0, false, true);

                attacker.PlaySound(0x20A);

                SpellHelper.Damage(this, defender, damage, 0, 0, 0, 0, 100);
            }
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
        }
    }
}
