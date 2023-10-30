using System;
using Server.Collections;
using Server.Mobiles;

namespace Server.Spells.Fourth
{
    public class ArchCureSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Arch Cure",
            "Vas An Nox",
            215,
            9061,
            Reagent.Garlic,
            Reagent.Ginseng,
            Reagent.MandrakeRoot
        );

        public ArchCureSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fourth;

        // Arch cure is now 1/4th of a second faster
        public override TimeSpan CastDelayBase => base.CastDelayBase - TimeSpan.FromSeconds(0.25);

        public void Target(IPoint3D p)
        {
            if (CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                var map = Caster.Map;
                if (map != null)
                {
                    using var pool = PooledRefQueue<Mobile>.Create();
                    var directTarget = p as Mobile;
                    var loc = new Point3D(p);

                    var feluccaRules = map.Rules == MapRules.FeluccaRules;

                    // You can target any living mobile directly, beneficial checks apply
                    if (directTarget != null && Caster.CanBeBeneficial(directTarget, false))
                    {
                        pool.Enqueue(directTarget);
                    }

                    foreach (var m in map.GetMobilesInRange(loc, 2))
                    {
                        if (m != directTarget && AreaCanTarget(m, feluccaRules))
                        {
                            pool.Enqueue(m);
                        }
                    }

                    Effects.PlaySound(loc, Caster.Map, 0x299);

                    var cured = 0;

                    while (pool.Count > 0)
                    {
                        var m = pool.Dequeue();

                        Caster.DoBeneficial(m);

                        var poison = m.Poison;

                        if (poison != null)
                        {
                            var chanceToCure = 10000 + (int)(Caster.Skills.Magery.Value * 75) -
                                               (poison.Level + 1) * 1750;
                            chanceToCure /= 100;
                            chanceToCure -= 1;

                            if (chanceToCure > Utility.Random(100) && m.CurePoison(Caster))
                            {
                                ++cured;
                            }
                        }

                        m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                        m.PlaySound(0x1E0);
                    }

                    if (cured > 0)
                    {
                        Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!
                    }
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, range: Core.ML ? 10 : 12);
        }

        private bool AreaCanTarget(Mobile target, bool feluccaRules)
        {
            /* Arch cure area effect won't cure aggressors, victims, murderers, criminals or monsters.
             * In Felucca, it will also not cure summons and pets.
             * For red players it will only cure themselves and guild members.
             */

            if (!Caster.CanBeBeneficial(target, false))
            {
                return false;
            }

            if (Core.AOS && target != Caster)
            {
                if (IsAggressor(target) || IsAggressed(target))
                {
                    return false;
                }

                if ((!IsInnocentTo(Caster, target) || !IsInnocentTo(target, Caster)) && !IsAllyTo(Caster, target))
                {
                    return false;
                }

                if (feluccaRules && target is not PlayerMobile)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsAggressor(Mobile m)
        {
            foreach (var info in Caster.Aggressors)
            {
                if (m == info.Attacker && !info.Expired)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAggressed(Mobile m)
        {
            foreach (var info in Caster.Aggressed)
            {
                if (m == info.Defender && !info.Expired)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInnocentTo(Mobile from, Mobile to) => Notoriety.Compute(from, to) == Notoriety.Innocent;

        private static bool IsAllyTo(Mobile from, Mobile to) => Notoriety.Compute(from, to) == Notoriety.Ally;
    }
}
