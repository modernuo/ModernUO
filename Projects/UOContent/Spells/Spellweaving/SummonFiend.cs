using System;
using Server.Engines.MLQuests;
using Server.Mobiles;

namespace Server.Spells.Spellweaving
{
    public class SummonFiendSpell : ArcaneSummon<ArcaneFiend>
    {
        private static readonly SpellInfo _info = new(
            "Summon Fiend",
            "Nylisstra",
            -1
        );

        public SummonFiendSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

        public override double RequiredSkill => 38.0;
        public override int RequiredMana => 10;

        public override int Sound => 0x216;

        public override bool CheckSequence()
        {
            // This is done after casting completes
            if (Caster is PlayerMobile mobile)
            {
                var context = MLQuestSystem.GetContext(mobile);

                if (context?.SummonFiend != true)
                {
                    mobile.SendLocalizedMessage(1074564); // You haven't demonstrated mastery to summon a fiend.
                    return false;
                }
            }

            return base.CheckSequence();
        }
    }
}
