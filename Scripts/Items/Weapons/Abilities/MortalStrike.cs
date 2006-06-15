using System;
using System.Collections;

namespace Server.Items
{
	/// <summary>
	/// The assassin's friend.
	/// A successful Mortal Strike will render its victim unable to heal any damage for several seconds. 
	/// Use a gruesome follow-up to finish off your foe.
	/// </summary>
	public class MortalStrike : WeaponAbility
	{
		public MortalStrike()
		{
		}

		public override int BaseMana{ get{ return 30; } }

		public static readonly TimeSpan PlayerDuration = TimeSpan.FromSeconds( 6.0 );
		public static readonly TimeSpan NPCDuration = TimeSpan.FromSeconds( 12.0 );

		public override void OnHit(Mobile attacker, Mobile defender, int damage)
		{
			if ( !Validate( attacker ) || !CheckMana( attacker, true ) )
				return;

			ClearCurrentAbility( attacker );

			attacker.SendLocalizedMessage( 1060086 ); // You deliver a mortal wound!
			defender.SendLocalizedMessage( 1060087 ); // You have been mortally wounded!

			defender.PlaySound( 0x1E1 );
			defender.FixedParticles( 0x37B9, 244, 25, 9944, 31, 0, EffectLayer.Waist );

			// Do not reset timer if one is already in place.
			if ( !IsWounded( defender ) )
				BeginWound( defender, defender.Player ? PlayerDuration : NPCDuration );
		}

		private static Hashtable m_Table = new Hashtable();

		public static bool IsWounded( Mobile m )
		{
			return m_Table.Contains( m );
		}

		public static void BeginWound( Mobile m, TimeSpan duration )
		{
			Timer t = (Timer)m_Table[m];

			if ( t != null )
				t.Stop();

			t = new InternalTimer( m, duration );
			m_Table[m] = t;

			t.Start();

			m.YellowHealthbar = true;
		}

		public static void EndWound( Mobile m )
		{
			Timer t = (Timer)m_Table[m];

			if ( t != null )
				t.Stop();

			m_Table.Remove( m );

			m.YellowHealthbar = false;
			m.SendLocalizedMessage( 1060208 ); // You are no longer mortally wounded.
		}

		private class InternalTimer : Timer
		{
			private Mobile m_Mobile;

			public InternalTimer( Mobile m, TimeSpan duration ) : base( duration )
			{
				m_Mobile = m;
				Priority = TimerPriority.TwoFiftyMS;
			}

			protected override void OnTick()
			{
				EndWound( m_Mobile );
			}
		}
	}
}