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
                        _ => TimeSpan.FromSeconds(2 * Caster.Skills.Magery.Fixed / 5.0)
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
            if (Core.AOS)
            {
                return TimeSpan.FromTicks(base.GetCastDelay().Ticks * 5);
            }

            return base.GetCastDelay() + TimeSpan.FromSeconds(6.0);
        }
    }
}
