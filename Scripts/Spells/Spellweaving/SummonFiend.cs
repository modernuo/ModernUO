using System;
using Server.Mobiles;

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
	}
}