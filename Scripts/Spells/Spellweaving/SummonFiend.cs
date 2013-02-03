using System;
using Server.Mobiles;
using Server.Engines.MLQuests;

namespace Server.Spells.Spellweaving
{
	public class SummonFiendSpell : ArcaneSummon<ArcaneFiend>
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Summon Fiend", "Nylisstra",
				-1
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 2.0 ); } }

		public override double RequiredSkill { get { return 38.0; } }
		public override int RequiredMana { get { return 10; } }

		public SummonFiendSpell( Mobile caster, Item scroll )
			: base( caster, scroll, m_Info )
		{
		}

		public override int Sound { get { return 0x216; } }

		public override bool CheckSequence()
		{
			Mobile caster = Caster;

			// This is done after casting completes
			if ( caster is PlayerMobile )
			{
				MLQuestContext context = MLQuestSystem.GetContext( (PlayerMobile)caster );

				if ( context == null || !context.SummonFiend )
				{
					caster.SendLocalizedMessage( 1074564 ); // You haven't demonstrated mastery to summon a fiend.
					return false;
				}
			}

			return base.CheckSequence();
		}
	}
}
