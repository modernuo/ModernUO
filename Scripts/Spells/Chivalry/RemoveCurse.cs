using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Targeting;
using Server.Spells.Necromancy;
using Server.Spells.Fourth;

namespace Server.Spells.Chivalry
{
	public class RemoveCurseSpell : PaladinSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Remove Curse", "Extermo Vomica",
				-1,
				9002
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.5 ); } }

		public override double RequiredSkill{ get{ return 5.0; } }
		public override int RequiredMana{ get{ return 20; } }
		public override int RequiredTithing{ get{ return 10; } }
		public override int MantraNumber{ get{ return 1060726; } } // Extermo Vomica

		public RemoveCurseSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( Mobile m )
		{
			if ( CheckBSequence( m ) )
			{
				SpellHelper.Turn( Caster, m );

				/* Attempts to remove all Curse effects from Target.
				 * Curses include Mage spells such as Clumsy, Weaken, Feeblemind and Paralyze
				 * as well as all Necromancer curses.
				 * Chance of removing curse is affected by Caster's Karma.
				 */

				int chance = 0;

				if ( Caster.Karma < -5000 )
					chance = 0;
				else if ( Caster.Karma < 0 )
					chance = (int) Math.Sqrt( 20000 + Caster.Karma ) - 122;
				else if ( Caster.Karma < 5625 )
					chance = (int) Math.Sqrt( Caster.Karma ) + 25;
				else
					chance = 100;

				if ( chance > Utility.Random( 100 ) )
				{
					m.PlaySound( 0xF6 );
					m.PlaySound( 0x1F7 );
					m.FixedParticles( 0x3709, 1, 30, 9963, 13, 3, EffectLayer.Head );

					IEntity from = new Entity( Serial.Zero, new Point3D( m.X, m.Y, m.Z - 10 ), Caster.Map );
					IEntity to = new Entity( Serial.Zero, new Point3D( m.X, m.Y, m.Z + 50 ), Caster.Map );
					Effects.SendMovingParticles( from, to, 0x2255, 1, 0, false, false, 13, 3, 9501, 1, 0, EffectLayer.Head, 0x100 );

					StatMod mod;

					mod = m.GetStatMod( "[Magic] Str Offset" );
					if ( mod != null && mod.Offset < 0 )
						m.RemoveStatMod( "[Magic] Str Offset" );

					mod = m.GetStatMod( "[Magic] Dex Offset" );
					if ( mod != null && mod.Offset < 0 )
						m.RemoveStatMod( "[Magic] Dex Offset" );

					mod = m.GetStatMod( "[Magic] Int Offset" );
					if ( mod != null && mod.Offset < 0 )
						m.RemoveStatMod( "[Magic] Int Offset" );

					m.Paralyzed = false;

					EvilOmenSpell.CheckEffect( m );
					StrangleSpell.RemoveCurse( m );
					CorpseSkinSpell.RemoveCurse( m );
					CurseSpell.RemoveEffect( m );

					BuffInfo.RemoveBuff( m, BuffIcon.Clumsy );
					BuffInfo.RemoveBuff( m, BuffIcon.FeebleMind );
					BuffInfo.RemoveBuff( m, BuffIcon.Weaken );
					BuffInfo.RemoveBuff( m, BuffIcon.MassCurse );

					// TODO: Should this remove blood oath? Pain spike?
				}
				else
				{
					m.PlaySound( 0x1DF );
				}
			}

			FinishSequence();
		}

		private class InternalTarget : Target
		{
			private RemoveCurseSpell m_Owner;

			public InternalTarget( RemoveCurseSpell owner ) : base( 12, false, TargetFlags.Beneficial )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is Mobile )
					m_Owner.Target( (Mobile) o );
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}