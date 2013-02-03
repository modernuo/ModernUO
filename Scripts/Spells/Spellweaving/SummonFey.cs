using System;
using Server.Mobiles;
using Server.Engines.MLQuests;

namespace Server.Spells.Spellweaving
{
	public class SummonFeySpell : ArcaneSummon<ArcaneFey>
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Summon Fey", "Alalithra",
				-1
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.5 ); } }

		public override double RequiredSkill { get { return 38.0; } }
		public override int RequiredMana { get { return 10; } }

		public SummonFeySpell( Mobile caster, Item scroll )
			: base( caster, scroll, m_Info )
		{
		}

		public override int Sound { get { return 0x217; } }

		public override bool CheckSequence()
		{
			Mobile caster = Caster;

			// This is done after casting completes
			if ( caster is PlayerMobile )
			{
				MLQuestContext context = MLQuestSystem.GetContext( (PlayerMobile)caster );

				if ( context == null || !context.SummonFey )
				{
					caster.SendLocalizedMessage( 1074563 ); // You haven't forged a friendship with the fey and are unable to summon their aid.
					return false;
				}
			}

			return base.CheckSequence();
		}
	}
}
