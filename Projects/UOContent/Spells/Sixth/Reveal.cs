using Server.Mobiles;

namespace Server.Spells.Sixth
{
    public class RevealSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Reveal",
            "Wis Quas",
            206,
            9002,
            Reagent.Bloodmoss,
            Reagent.SulfurousAsh
        );

        public RevealSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Sixth;

        public void Target(IPoint3D p)
        {
            if (CheckSequence())
            {
                SpellHelper.Turn(Caster, p);
                SpellHelper.GetSurfaceTop(ref p);
                var map = Caster.Map;

                if (map != null)
                {
                    var eable = map.GetMobilesInRange(
                        new Point3D(p),
                        1 + (int)(Caster.Skills.Magery.Value / 20.0)
                    );

                    foreach (var m in eable)
                    {
                        if (m is ShadowKnight && (m.X != p.X || m.Y != p.Y))
                        {
                            continue;
                        }

                        if (!m.Hidden
                            || m.AccessLevel > AccessLevel.Player && Caster.AccessLevel <= m.AccessLevel
                            || !CheckDifficulty(Caster, m))
                        {
                            continue;
                        }

                        m.RevealingAction();

                        m.FixedParticles(0x375A, 9, 20, 5049, EffectLayer.Head);
                        m.PlaySound(0x1FD);
                    }
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, range: Core.ML ? 10 : 12);
        }

        // Reveal uses magery and detect hidden vs. hide and stealth
        private static bool CheckDifficulty(Mobile from, Mobile m)
        {
            // Reveal always reveals vs. invisibility spell
            if (!Core.AOS || InvisibilitySpell.HasTimer(m))
            {
                return true;
            }

            var magery = from.Skills.Magery.Fixed;
            var detectHidden = from.Skills.DetectHidden.Fixed;

            var hiding = m.Skills.Hiding.Fixed;
            var stealth = m.Skills.Stealth.Fixed;
            var divisor = hiding + stealth;

            int chance;
            if (divisor > 0)
            {
                chance = 50 * (magery + detectHidden) / divisor;
            }
            else
            {
                chance = 100;
            }

            return chance > Utility.Random(100);
        }
    }
}
