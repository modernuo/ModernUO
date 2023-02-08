using System;
using Server.Mobiles;

namespace Server.Spells.Fifth
{
    public class BladeSpiritsSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Blade Spirits",
            "In Jux Hur Ylem",
            266,
            9040,
            false,
            Reagent.BlackPearl,
            Reagent.MandrakeRoot,
            Reagent.Nightshade
        );

        public BladeSpiritsSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fifth;

        public void Target(IPoint3D p)
        {
            var map = Caster.Map;

            SpellHelper.GetSurfaceTop(ref p);

            if (map?.CanSpawnMobile(p.X, p.Y, p.Z) != true)
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                var duration = TimeSpan.FromSeconds(Core.AOS ? 120 : Utility.Random(80, 40));
                BaseCreature.Summon(new BladeSpirits(), false, Caster, new Point3D(p), 0x212, duration);
            }

            FinishSequence();
        }

        public override TimeSpan GetCastDelay()
        {
            var scalar = Core.Expansion switch
            {
                >= Expansion.SE  => 3,
                >= Expansion.AOS => 5,
                _                => 4
            };

            var delay = base.GetCastDelay() * scalar;

            // SA made everything 0.25s slower, but that is applied after the scalar
            // So remove 0.25 * scalar to compensate
            if (Core.SA)
            {
                delay -= TimeSpan.FromSeconds(0.25 * scalar);
            }

            return delay;
        }

        public override bool CheckCast()
        {
            if (!base.CheckCast())
            {
                return false;
            }

            if (Caster.Followers + (Core.SE ? 2 : 1) > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, retryOnLOS: true);
        }
    }
}
