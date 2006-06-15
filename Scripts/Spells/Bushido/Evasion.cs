using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Bushido
{
	public class Evasion : SamuraiSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Evasion", null,
				SpellCircle.First, // 0 + 0.25 = 0.25s base cast delay
				-1,
				9002
			);

		public override double RequiredSkill{ get{ return 60.0; } }
		public override int RequiredMana{ get{ return 10; } }

		public override bool CheckCast()
		{
			if ( Caster.FindItemOnLayer( Layer.TwoHanded ) as BaseShield != null )
				return base.CheckCast();

			if ( Caster.FindItemOnLayer( Layer.OneHanded ) as BaseWeapon != null )
				return base.CheckCast();

			if ( Caster.FindItemOnLayer( Layer.TwoHanded ) as BaseWeapon != null )
				return base.CheckCast();

			Caster.SendLocalizedMessage( 1062944 ); // You must have a weapon or a shield equipped to use this ability!
			return false;
		}

		public Evasion( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnBeginCast()
		{
			base.OnBeginCast();

			Caster.FixedEffect( 0x37C4, 10, 7, 4, 3 );
		}

		public override void OnCast()
		{
			if ( CheckSequence() )
			{
				Caster.SendLocalizedMessage( 1063120 ); // You feel that you might be able to deflect any attack!

				Caster.FixedParticles( 0x376A, 1, 20, 0x7F5, 0x960, 0x3, EffectLayer.Waist );
				Caster.PlaySound( 0x51B );

				OnCastSuccessful( Caster );

				BeginEvasion( Caster );
			}

			FinishSequence();
		}

		private static Hashtable m_Table = new Hashtable();

		public static bool IsEvading( Mobile m )
		{
			return m_Table.Contains( m );
		}

		public static void BeginEvasion( Mobile m )
		{
			Timer t = (Timer)m_Table[m];

			if ( t != null )
				t.Stop();

			t = new InternalTimer( m );

			m_Table[m] = t;

			t.Start();
		}

		public static void EndEvasion( Mobile m )
		{
			Timer t = (Timer)m_Table[m];

			if ( t != null )
				t.Stop();

			m_Table.Remove( m );

			OnEffectEnd( m, typeof( Evasion ) );
		}

		private class InternalTimer : Timer
		{
			private Mobile m_Mobile;

			public InternalTimer( Mobile m ) : base( TimeSpan.FromSeconds( 8.0 ) )
			{
				m_Mobile = m;
				Priority = TimerPriority.TwoFiftyMS;
			}

			protected override void OnTick()
			{
				EndEvasion( m_Mobile );
				m_Mobile.SendLocalizedMessage( 1063121 ); // You no longer feel that you could deflect any attack.
			}
		}
	}
}