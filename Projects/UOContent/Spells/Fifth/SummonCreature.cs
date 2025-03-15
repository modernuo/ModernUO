using System;
using Server.Mobiles;

namespace Server.Spells.Fifth
{
    public class SummonCreatureSpell : MagerySpell
    {
        private static readonly SpellInfo _info = new(
            "Summon Creature",
            "Kal Xen",
            16,
            true,
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
            typeof(Walrus),
            typeof(Scorpion),
            typeof(GiantSerpent),
            typeof(Alligator),
            typeof(GreyWolf),
            typeof(Slime),
            typeof(Eagle),
            typeof(Gorilla),
            typeof(SnowLeopard),
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

                    creature.ControlSlots = 2;

                    //var duration = Core.Expansion switch
                    //{
                    //    Expansion.None => TimeSpan.FromSeconds(2.0),
                    //    _ => TimeSpan.FromSeconds((int)Caster.Skills.Magery.Value * 4)
                    //};

                    SpellHelper.Summon(creature, Caster, 0x215, TimeSpan.FromSeconds(0), true, true);
                }
                catch
                {
                    // ignored
                }
            }

            FinishSequence();
        }
    }
}
