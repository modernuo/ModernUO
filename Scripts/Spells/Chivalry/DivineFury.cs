using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.Chivalry
{
	public class DivineFurySpell : PaladinSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Divine Fury", "Divinum Furis",
				-1,
				9002
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.0 ); } }

		public override double RequiredSkill{ get{ return 25.0; } }
		public override int RequiredMana{ get{ return 15; } }
		public override int RequiredTithing{ get{ return 10; } }
		public override int MantraNumber{ get{ return 1060722; } } // Divinum Furis
		public override bool BlocksMovement{ get{ return false; } }

		public DivineFurySpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			if ( CheckSequence() )
			{
				Caster.PlaySound( 0x20F );
				Caster.PlaySound( Caster.Body.IsFemale ? 0x338 : 0x44A );
				Caster.FixedParticles( 0x376A, 1, 31, 9961, 1160, 0, EffectLayer.Waist );
				Caster.FixedParticles( 0x37C4, 1, 31, 9502, 43, 2, EffectLayer.Waist );

				Caster.Stam = Caster.StamMax;

				Timer t = (Timer)m_Table[Caster];

				if ( t != null )
					t.Stop();

				int delay = ComputePowerValue( 10 );

				// TODO: Should caps be applied?
				if ( delay < 7 )
					delay = 7;
				else if ( delay > 24 )
					delay = 24;

				m_Table[Caster] = t = Timer.DelayCall( TimeSpan.FromSeconds( delay ), new TimerStateCallback( Expire_Callback ), Caster );
				Caster.Delta( MobileDelta.WeaponDamage );
			}

			FinishSequence();
		}

		private static Hashtable m_Table = new Hashtable();

		public static bool UnderEffect( Mobile m )
		{
			return m_Table.Contains( m );
		}

		private static void Expire_Callback( object state )
		{
			Mobile m = (Mobile)state;

			m_Table.Remove( m );

			m.Delta( MobileDelta.WeaponDamage );
			m.PlaySound( 0xF8 );
		}
	}
}