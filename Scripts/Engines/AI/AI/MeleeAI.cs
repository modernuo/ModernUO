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
	public class MeleeAI : BaseAI
	{
		public MeleeAI(BaseCreature m) : base (m)
		{
		}

		public override bool DoActionWander()
		{
			m_Mobile.DebugSay( "I have no combatant" );

			if ( AcquireFocusMob( m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true ) )
			{
				if ( m_Mobile.Debug )
					m_Mobile.DebugSay( "I have detected {0}, attacking", m_Mobile.FocusMob.Name );

				m_Mobile.Combatant = m_Mobile.FocusMob;
				Action = ActionType.Combat;
			}
			else
			{
				base.DoActionWander();
			}

			return true;
		}

		public override bool DoActionCombat()
		{
			Mobile combatant = m_Mobile.Combatant;

			if ( combatant == null || combatant.Deleted || combatant.Map != m_Mobile.Map || !combatant.Alive || combatant.IsDeadBondedPet )
			{
				m_Mobile.DebugSay( "My combatant is gone, so my guard is up" );

				Action = ActionType.Guard;

				return true;
			}

			if ( !m_Mobile.InRange( combatant, m_Mobile.RangePerception ) )
			{
				// They are somewhat far away, can we find something else?

				if ( AcquireFocusMob( m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true ) )
				{
					m_Mobile.Combatant = m_Mobile.FocusMob;
					m_Mobile.FocusMob = null;
				}
				else if ( !m_Mobile.InRange( combatant, m_Mobile.RangePerception * 3 ) )
				{
					m_Mobile.Combatant = null;
				}

				combatant = m_Mobile.Combatant;

				if ( combatant == null )
				{
					m_Mobile.DebugSay( "My combatant has fled, so I am on guard" );
					Action = ActionType.Guard;

					return true;
				}
			}

			/*if ( !m_Mobile.InLOS( combatant ) )
			{
				if ( AcquireFocusMob( m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true ) )
				{
					m_Mobile.Combatant = combatant = m_Mobile.FocusMob;
					m_Mobile.FocusMob = null;
				}
			}*/

			if ( MoveTo( combatant, true, m_Mobile.RangeFight ) )
			{
				m_Mobile.Direction = m_Mobile.GetDirectionTo( combatant );
			}
			else if ( AcquireFocusMob( m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true ) )
			{
				if ( m_Mobile.Debug )
					m_Mobile.DebugSay( "My move is blocked, so I am going to attack {0}", m_Mobile.FocusMob.Name );

				m_Mobile.Combatant = m_Mobile.FocusMob;
				Action = ActionType.Combat;

				return true;
			}
			else if ( m_Mobile.GetDistanceToSqrt( combatant ) > m_Mobile.RangePerception + 1 )
			{
				if ( m_Mobile.Debug )
					m_Mobile.DebugSay( "I cannot find {0}, so my guard is up", combatant.Name );

				Action = ActionType.Guard;

				return true;
			}
			else
			{
				if ( m_Mobile.Debug )
					m_Mobile.DebugSay( "I should be closer to {0}", combatant.Name );
			}

			if ( !m_Mobile.Controlled && !m_Mobile.Summoned && !m_Mobile.IsParagon )
			{
				if ( m_Mobile.Hits < m_Mobile.HitsMax * 20/100 )
				{
					// We are low on health, should we flee?

					bool flee = false;

					if ( m_Mobile.Hits < combatant.Hits )
					{
						// We are more hurt than them

						int diff = combatant.Hits - m_Mobile.Hits;

						flee = ( Utility.Random( 0, 100 ) < (10 + diff) ); // (10 + diff)% chance to flee
					}
					else
					{
						flee = Utility.Random( 0, 100 ) < 10; // 10% chance to flee
					}

					if ( flee )
					{
						if ( m_Mobile.Debug )
							m_Mobile.DebugSay( "I am going to flee from {0}", combatant.Name );

						Action = ActionType.Flee;
					}
				}
			}

			return true;
		}

		public override bool DoActionGuard()
		{
			if ( AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true ) )
			{
				if ( m_Mobile.Debug )
					m_Mobile.DebugSay( "I have detected {0}, attacking", m_Mobile.FocusMob.Name );

				m_Mobile.Combatant = m_Mobile.FocusMob;
				Action = ActionType.Combat;
			}
			else
			{
				base.DoActionGuard();
			}

			return true;
		}

		public override bool DoActionFlee()
		{
			if ( m_Mobile.Hits > m_Mobile.HitsMax/2 )
			{
				m_Mobile.DebugSay( "I am stronger now, so I will continue fighting" );
				Action = ActionType.Combat;
			}
			else
			{
				m_Mobile.FocusMob = m_Mobile.Combatant;
				base.DoActionFlee();
			}

			return true;
		}
	}
}