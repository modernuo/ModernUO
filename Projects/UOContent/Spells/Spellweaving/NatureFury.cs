using System;
using Server.Mobiles;
using Server.Regions;
using Server.Targeting;

namespace Server.Spells.Spellweaving
{
    public class NatureFurySpell : ArcanistSpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Nature's Fury",
            "Rauvvrae",
            -1,
            false
        );

        public NatureFurySpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 0.0;
        public override int RequiredMana => 24;

        public void Target(IPoint3D point)
        {
            var p = new Point3D(point);
            var map = Caster.Map;

            if (map == null)
            {
                return;
            }

            if (Region.Find(p, map).GetRegion<HouseRegion>()?.House?.IsFriend(Caster) == false)
            {
                return;
            }

            if (!map.CanSpawnMobile(p.X, p.Y, p.Z))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                var duration = TimeSpan.FromSeconds(Caster.Skills.Spellweaving.Value / 24 + 25 + FocusLevel * 2);

                var nf = new NatureFury();
                BaseCreature.Summon(nf, false, Caster, p, 0x5CB, duration);

                new InternalTimer(nf).Start();
            }

            FinishSequence();
        }

        public override bool CheckCast()
        {
            if (!base.CheckCast())
            {
                return false;
            }

            if (Caster.Followers + 1 > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, TargetFlags.None, 10);
        }

        private class InternalTimer : Timer
        {
            private readonly NatureFury _natureFury;

            public InternalTimer(NatureFury nf) : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0)) =>
                _natureFury = nf;

            protected override void OnTick()
            {
                if (_natureFury.Deleted || !_natureFury.Alive || _natureFury.DamageMin > 20)
                {
                    Stop();
                }
                else
                {
                    ++_natureFury.DamageMin;
                    ++_natureFury.DamageMax;
                }
            }
        }
    }
}
