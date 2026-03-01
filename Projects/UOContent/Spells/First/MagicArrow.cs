using System;
using Server.Targeting;

namespace Server.Spells.First
{
    public class MagicArrowSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Magic Arrow",
            "In Por Ylem",
            212,
            9041,
            Reagent.SulfurousAsh
        );

        public MagicArrowSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.First;

        public override Type[] DelayedDamageSpellFamilyStacking => AOSNoDelayedDamageStackingSelf;

        public override bool DelayedDamage => true;

        public void Target(Mobile defender)
        {
            if (CheckHSequence(defender))
            {
                var attacker = Caster;

                SpellHelper.Turn(attacker, defender);

                if (Core.SA && HasDelayedDamageContext(defender))
                {
                    DoHurtFizzle();
                    return;
                }

                SpellHelper.CheckReflect((int)Circle, ref attacker, ref defender);

                double damage;

                if (Core.AOS)
                {
                    damage = GetNewAosDamage(10, 1, 4, defender);
                }
                else
                {
                    damage = Utility.Random(4, 4);

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
                    zOffset = attacker.Mount != null ? 20 : 12;
                }
                else if (deltaX > 0 && deltaY < 0)
                {
                    xOffset = 1;
                    yOffset = -1;
                    zOffset = attacker.Mount != null ? 18 : 12;
                }
                else if (deltaX > 0 && deltaY == 0)
                {
                    xOffset = 1;
                    yOffset = 0;
                    zOffset = attacker.Mount != null ? 20 : 12;
                }
                else if (deltaX > 0 && deltaY > 0)
                {
                    xOffset = 0;
                    yOffset = 1;
                    zOffset = attacker.Mount != null ? 20 : 12;
                }
                else if (deltaX == 0 && deltaY > 0)
                {
                    xOffset = 0;
                    yOffset = 1;
                    zOffset = attacker.Mount != null ? 23 : 15;
                
                }
                else if (deltaX < 0 && deltaY > 0)
                {
                    xOffset = -1;
                    yOffset = 1;
                    zOffset = attacker.Mount != null ? 20 : 12;
                }
                else if (deltaX < 0 && deltaY == 0)
                {
                    xOffset = -1;
                    yOffset = 0;
                    zOffset = attacker.Mount != null ? 20 : 12;
                }
                else if (deltaX < 0 && deltaY < 0)
                {
                    xOffset = 1;
                    yOffset = 0;
                    zOffset = attacker.Mount != null ? 20 : 12;

                }
            
                Point3D from = new(attacker.X + xOffset, attacker.Y + yOffset, attacker.Z + zOffset);
                Point3D to = new(defender.X + xOffset, defender.Y + yOffset, defender.Z + zOffset);
            
                Effects.SendMovingEffect(new Entity(Serial.Zero, from, attacker.Map),
                    new Entity(Serial.Zero, to, defender.Map), 0x36E4, 5, 0, false, false);

                attacker.PlaySound(0x1E5);

                SpellHelper.Damage(this, defender, damage, 0, 100, 0, 0, 0);
            }
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
        }
    }
}
