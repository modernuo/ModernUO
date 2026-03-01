using Server.Targeting;

namespace Server.Spells.Third
{
    public class FireballSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Fireball",
            "Vas Flam",
            203,
            9041,
            Reagent.BlackPearl
        );

        public FireballSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Third;

        public override bool DelayedDamage => true;

        public void Target(Mobile defender)
        {
            if (CheckHSequence(defender))
            {
                var attacker = Caster;

                SpellHelper.Turn(attacker, defender);

                SpellHelper.CheckReflect((int)Circle, ref attacker, ref defender);

                double damage;

                if (Core.AOS)
                {
                    damage = GetNewAosDamage(19, 1, 5, defender);
                }
                else
                {
                    damage = Utility.Random(10, 7);

                    if (CheckResisted(defender))
                    {
                        damage *= 0.75;

                        defender.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

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
                    xOffset = 1;
                    yOffset = -1;
                    zOffset = attacker.Mount != null ? 9 : 4;
                }
                else if (deltaX > 0 && deltaY == 0)
                {
                    xOffset = 1;
                    yOffset = 0;
                    zOffset = attacker.Mount != null ? 9 : 4;
                }
                else if (deltaX > 0 && deltaY > 0)
                {
                    xOffset = 0;
                    yOffset = 1;
                    zOffset = attacker.Mount != null ? 6 : 3;
                }
                else if (deltaX == 0 && deltaY > 0)
                {
                    xOffset = 0;
                    yOffset = 1;
                    zOffset = attacker.Mount != null ? 16 : 8;
                
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
                    zOffset = attacker.Mount != null ? 16 : 8;
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
                    new Entity(Serial.Zero, to, defender.Map), 0x36D4, 7, 0, false, true);

                attacker.PlaySound(Core.AOS ? 0x15E : 0x44B);

                SpellHelper.Damage(this, defender, damage, 0, 100, 0, 0, 0);
            }
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
        }
    }
}
