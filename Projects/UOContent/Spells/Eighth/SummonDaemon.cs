using System;
using Server.Mobiles;

namespace Server.Spells.Eighth
{
    public class SummonDaemonSpell : MagerySpell
    {
        private static readonly SpellInfo _info = new(
            "Summon Daemon",
            "Kal Vas Xen Corp",
            269,
            9050,
            false,
            Reagent.Bloodmoss,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk,
            Reagent.SulfurousAsh
        );

        public SummonDaemonSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Eighth;

        public override bool CheckCast()
        {
            if (!base.CheckCast())
            {
                return false;
            }

            if (Caster.Followers + (Core.SE ? 4 : 5) > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            if (CheckSequence())
            {
                var duration = Core.Expansion switch
                {
                    Expansion.None => TimeSpan.FromSeconds(Caster.Skills.Magery.Value),
                    // T2A -> Current
                    _ => TimeSpan.FromSeconds(4 * Math.Min(5, Caster.Skills.Magery.Value)),
                };

                if (Core.AOS) /* Why two diff daemons? TODO: solve this */
                {
                    BaseCreature m_Daemon = new SummonedDaemon();
                    SpellHelper.Summon(m_Daemon, Caster, 0x216, duration, false, false);
                    m_Daemon.FixedParticles(0x3728, 8, 20, 5042, EffectLayer.Head);
                }
                else
                {
                    SpellHelper.Summon(new Daemon(), Caster, 0x216, duration, false, false);
                }
            }

            FinishSequence();
        }
    }
}
