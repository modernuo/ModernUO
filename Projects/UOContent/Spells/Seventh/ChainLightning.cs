using System.Collections.Generic;
using System.Linq;
using Collections.Pooled;
using Server.Targeting;

namespace Server.Spells.Seventh
{
    public class ChainLightningSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo m_Info = new(
            "Chain Lightning",
            "Vas Ort Grav",
            209,
            9022,
            false,
            Reagent.BlackPearl,
            Reagent.Bloodmoss,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh
        );

        public ChainLightningSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Seventh;

        public override bool DelayedDamage => true;

        public void Target(IPoint3D p)
        {
            if (!Caster.CanSee(p))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                if (p is Item item)
                {
                    p = item.GetWorldLocation();
                }

                var map = Caster.Map;

                if (map == null)
                {
                    FinishSequence();
                    return;
                }

                var playerVsPlayer = false;

                var eable = map.GetMobilesInRange(new Point3D(p), 2);
                using var targets = eable.Where(
                    m =>
                    {
                        if (Core.AOS && (m == Caster || !Caster.InLOS(m)) ||
                            !SpellHelper.ValidIndirectTarget(Caster, m) ||
                            !Caster.CanBeHarmful(m, false))
                        {
                            return false;
                        }

                        if (m.Player)
                        {
                            playerVsPlayer = true;
                        }

                        return true;
                    }
                ).ToPooledList();

                eable.Free();

                if (targets.Count > 0)
                {
                    double damage = Core.AOS
                        ? GetNewAosDamage(51, 1, 5, playerVsPlayer)
                        : (double)Utility.Random(27, 22) / targets.Count;

                    if (Core.AOS && targets.Count > 2)
                    {
                        damage = damage * 2 / targets.Count;
                    }

                    for (var i = 0; i < targets.Count; ++i)
                    {
                        var toDeal = damage;
                        var m = targets[i];

                        if (!Core.AOS && CheckResisted(m))
                        {
                            toDeal *= 0.5;

                            m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                        }

                        toDeal *= GetDamageScalar(m);
                        Caster.DoHarmful(m);
                        SpellHelper.Damage(this, m, toDeal, 0, 0, 0, 0, 100);

                        m.BoltEffect(0);
                    }
                }
                else
                {
                    Caster.PlaySound(0x29);
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, TargetFlags.None, Core.ML ? 10 : 12);
        }
    }
}
