using System;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Eighth
{
	public class SummonDaemonSpell : MagerySpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Summon Daemon", "Kal Vas Xen Corp",
				269,
				9050,
				false,
				Reagent.Bloodmoss,
				Reagent.MandrakeRoot,
				Reagent.SpidersSilk,
				Reagent.SulfurousAsh
			);

		public override SpellCircle Circle { get { return SpellCircle.Eighth; } }

		public SummonDaemonSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override bool CheckCast()
		{
			if ( !base.CheckCast() )
				return false;

			if ( (Caster.Followers + (Core.SE ? 4 : 5)) > Caster.FollowersMax )
			{
				Caster.SendLocalizedMessage( 1049645 ); // You have too many followers to summon that creature.
				return false;
			}

			return true;
		}

		public override void OnCast()
		{
			if ( CheckSequence() )
			{
				TimeSpan duration = TimeSpan.FromSeconds( (2 * Caster.Skills.Magery.Fixed) / 5 );

				if ( Core.AOS )
					SpellHelper.Summon( new SummonedDaemon(), Caster, 0x216, duration, false, false );
				else
					SpellHelper.Summon( new Daemon(), Caster, 0x216, duration, false, false );
			}

			FinishSequence();
		}
	}
}