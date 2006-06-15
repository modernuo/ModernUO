using System;
using System.Collections;
using Server.Targeting;
using Server.Network;

//
// This is a first simple AI
//
//

namespace Server.Mobiles
{
	public class VendorAI : BaseAI
	{
		public VendorAI(BaseCreature m) : base (m)
		{
		}

		public override bool DoActionWander()
		{
			m_Mobile.DebugSay( "I'm fine" );

			if ( m_Mobile.Combatant != null )
			{
				if ( m_Mobile.Debug )
					m_Mobile.DebugSay( "{0} is attacking me", m_Mobile.Combatant.Name );

				m_Mobile.Say( Utility.RandomList( 1005305, 501603 ) );

				Action = ActionType.Flee;
			}
			else
			{
				if ( m_Mobile.FocusMob != null )
				{
					if ( m_Mobile.Debug )
						m_Mobile.DebugSay( "{0} has talked to me", m_Mobile.FocusMob.Name );

					Action = ActionType.Interact;
				}
				else
				{
					m_Mobile.Warmode = false;

					base.DoActionWander();
				}
			}

			return true;
		}

		public override bool DoActionInteract()
		{
			Mobile customer = m_Mobile.FocusMob;

			if ( m_Mobile.Combatant != null )
			{
				if ( m_Mobile.Debug )
					m_Mobile.DebugSay( "{0} is attacking me", m_Mobile.Combatant.Name );

				m_Mobile.Say( Utility.RandomList( 1005305, 501603 ) );

				Action = ActionType.Flee;
				
				return true;
			}

			if ( customer == null || customer.Deleted || customer.Map != m_Mobile.Map )
			{
				m_Mobile.DebugSay( "My customer have disapeared" );
				m_Mobile.FocusMob = null;

				Action = ActionType.Wander;
			}
			else
			{
				if ( customer.InRange( m_Mobile, m_Mobile.RangeFight ) )
				{
					if ( m_Mobile.Debug )
						m_Mobile.DebugSay( "I am with {0}", customer.Name );

					m_Mobile.Direction = m_Mobile.GetDirectionTo( customer );
				}
				else
				{
					if ( m_Mobile.Debug )
						m_Mobile.DebugSay( "{0} is gone", customer.Name );

					m_Mobile.FocusMob = null;

					Action = ActionType.Wander;	
				}
			}

			return true;
		}

		public override bool DoActionGuard()
		{
			m_Mobile.FocusMob = m_Mobile.Combatant;
			return base.DoActionGuard();
		}

		public override bool HandlesOnSpeech( Mobile from )
		{
			if ( from.InRange( m_Mobile, 4 ) )
				return true;

			return base.HandlesOnSpeech( from );
		}

		// Temporary 
		public override void OnSpeech( SpeechEventArgs e )
		{
			base.OnSpeech( e );
 
			Mobile from = e.Mobile;
 
			if ( m_Mobile is BaseVendor && from.InRange( m_Mobile, Core.AOS ? 1 : 4 ) && !e.Handled )
			{
				if ( e.HasKeyword( 0x14D ) ) // *vendor sell*
				{
					e.Handled = true;

					((BaseVendor)m_Mobile).VendorSell( from );
					m_Mobile.FocusMob = from;
				}
				else if ( e.HasKeyword( 0x3C ) )
				{
					e.Handled = true;

					((BaseVendor)m_Mobile).VendorBuy( from );
					m_Mobile.FocusMob = from;
				}
				else if ( WasNamed( e.Speech ) )
				{
					e.Handled = true;

					if ( e.HasKeyword( 0x177 ) ) // *sell*
						((BaseVendor)m_Mobile).VendorSell( from );
					else if ( e.HasKeyword( 0x171 ) ) // *buy*
						((BaseVendor)m_Mobile).VendorBuy( from );

					m_Mobile.FocusMob = from;
				}
			}
		}
	}
}