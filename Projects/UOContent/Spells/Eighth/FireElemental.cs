using System;
using Server.Mobiles;

namespace Server.Spells.Eighth
{
    public class FireElementalSpell : MagerySpell
    {
        private static readonly SpellInfo _info = new(
            "Fire Elemental",
            "Kal Vas Xen Flam",
            269,
            9050,
            false,
            Reagent.Bloodmoss,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk,
            Reagent.SulfurousAsh
        );

        public FireElementalSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Eighth;

        public override bool CheckCast()
        {
            if (!base.CheckCast())
            {
                return false;
            }

            if (Caster.Followers + 4 > Caster.FollowersMax)
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
                    _ => TimeSpan.FromSeconds(4 * Math.Max(5, Caster.Skills.Magery.Value)),
                };

                if (Core.AOS)
                {
                    SpellHelper.Summon(new SummonedFireElemental(), Caster, 0x217, duration, false, false);
                }
                else
                {
                    SpellHelper.Summon(new FireElemental(), Caster, 0x217, duration, false, false);
                }
            }

            FinishSequence();
        }
    }
}
