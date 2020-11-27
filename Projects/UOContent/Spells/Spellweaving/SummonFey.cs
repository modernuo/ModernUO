using System;
using Server.Engines.MLQuests;
using Server.Mobiles;

namespace Server.Spells.Spellweaving
{
    public class SummonFeySpell : ArcaneSummon<ArcaneFey>
    {
        private static readonly SpellInfo m_Info = new(
            "Summon Fey",
            "Alalithra",
            -1
        );

        public SummonFeySpell(Mobile caster, Item scroll = null)
            : base(caster, scroll, m_Info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 38.0;
        public override int RequiredMana => 10;

        public override int Sound => 0x217;

        public override bool CheckSequence()
        {
            var caster = Caster;

            // This is done after casting completes
            if (caster is PlayerMobile mobile)
            {
                var context = MLQuestSystem.GetContext(mobile);

                if (context?.SummonFey != true)
                {
                    mobile.SendLocalizedMessage(
                        1074563
                    ); // You haven't forged a friendship with the fey and are unable to summon their aid.
                    return false;
                }
            }

            return base.CheckSequence();
        }
    }
}
