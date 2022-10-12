using System;
using Server.Mobiles;

namespace Server.Spells.Mysticism
{
    public class AnimatedWeaponSpell : MysticSpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Animated Weapon",
            "In Jux Por Ylem",
            -1,
            9002,
            Reagent.Bone,
            Reagent.BlackPearl,
            Reagent.MandrakeRoot,
            Reagent.Nightshade
        );

        public AnimatedWeaponSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fourth;

        public void Target(IPoint3D p)
        {
            if (Caster.Followers + 4 > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                return;
            }

            var map = Caster.Map;

            SpellHelper.GetSurfaceTop(ref p);

            if (map == null || Caster.Player && !map.CanSpawnMobile(p.X, p.Y, p.Z))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                var level = (int)((GetBaseSkill(Caster) + GetDamageSkill(Caster)) / 2.0);

                var duration = TimeSpan.FromSeconds(10 + level);

                var summon = new AnimatedWeapon(Caster, level);
                BaseCreature.Summon(summon, false, Caster, new Point3D(p), 0x212, duration);

                summon.PlaySound(0x64A);

                Effects.SendTargetParticles(summon, 0x3728, 10, 10, 0x13AA, (EffectLayer)255);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this);
        }
    }
}
