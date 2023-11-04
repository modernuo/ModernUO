using System;
using Server.Mobiles;
using Server.Utilities;

namespace Server.Spells.Fifth
{
    public class SummonCreatureSpell : MagerySpell
    {
        private static readonly SpellInfo _info = new(
            "Summon Creature",
            "Kal Xen",
            16,
            false,
            Reagent.Bloodmoss,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk
        );

        // NOTE: Creature list based on 1hr of summon/release on OSI.

        private static readonly Type[] m_Types =
        {
            typeof(PolarBear),
            typeof(GrizzlyBear),
            typeof(BlackBear),
            typeof(Horse),
            typeof(Walrus),
            typeof(Chicken),
            typeof(Scorpion),
            typeof(GiantSerpent),
            typeof(Llama),
            typeof(Alligator),
            typeof(GreyWolf),
            typeof(Slime),
            typeof(Eagle),
            typeof(Gorilla),
            typeof(SnowLeopard),
            typeof(Pig),
            typeof(Hind),
            typeof(Rabbit)
        };

        public SummonCreatureSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fifth;

        public override bool CheckCast()
        {
            if (!base.CheckCast())
            {
                return false;
            }

            if (Caster.Followers + 2 > Caster.FollowersMax)
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
                try
                {
                    var creature = m_Types.RandomElement().CreateInstance<BaseCreature>();

                    // creature.ControlSlots = 2;

                    var duration = Core.Expansion switch
                    {
                        Expansion.None => TimeSpan.FromSeconds(Caster.Skills.Magery.Value),
                        _ => TimeSpan.FromSeconds((int)Caster.Skills.Magery.Value * 4)
                    };

                    SpellHelper.Summon(creature, Caster, 0x215, duration, false, false);
                }
                catch
                {
                    // ignored
                }
            }

            FinishSequence();
        }

        public override TimeSpan GetCastDelay()
        {
            var delay = base.GetCastDelay() * (Core.AOS ? 5 : 4);

            // SA made everything 0.25 slower, but that is applied after the scalar
            // So remove 0.25 * 5 to compensate
            if (Core.SA)
            {
                delay -= TimeSpan.FromSeconds(1.25);
            }

            return delay;
        }
    }
}
