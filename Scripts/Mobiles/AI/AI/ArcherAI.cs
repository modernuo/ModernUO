using System;
using System.Collections;
using Server.Targeting;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Mobiles
{
	public class ArcherAI : BaseAI
	{
		public ArcherAI(BaseCreature m) : base (m)
		{
		}

		public override bool DoActionWander()
		{
			m_Mobile.DebugSay( "I have no combatant" );

			if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
			{
				if ( m_Mobile.Debug )
					m_Mobile.DebugSay( "I have detected {0} and I will attack", m_Mobile.FocusMob.Name );

				m_Mobile.Combatant = m_Mobile.FocusMob;
				Action = ActionType.Combat;
			}
			else
			{
				return base.DoActionWander();
			}

			return true;
		}

		public override bool DoActionCombat()
		{
			if ( m_Mobile.Combatant == null || m_Mobile.Combatant.Deleted || !m_Mobile.Combatant.Alive || m_Mobile.Combatant.IsDeadBondedPet )
			{
				m_Mobile.DebugSay("My combatant is deleted");
				Action = ActionType.Guard;
				return true;
			}

			if ( (m_Mobile.LastMoveTime + TimeSpan.FromSeconds( 1.0 )) < DateTime.Now )
			{
				if (WalkMobileRange(m_Mobile.Combatant, 1, true, m_Mobile.RangeFight, m_Mobile.Weapon.MaxRange))
				{
					// Be sure to face the combatant
					m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant.Location);
				}
				else
				{
					if ( m_Mobile.Combatant != null )
					{
						if ( m_Mobile.Debug )
							m_Mobile.DebugSay( "I am still not in range of {0}", m_Mobile.Combatant.Name);

						if ( (int) m_Mobile.GetDistanceToSqrt( m_Mobile.Combatant ) > m_Mobile.RangePerception + 1 )
						{
							if ( m_Mobile.Debug )
								m_Mobile.DebugSay( "I have lost {0}", m_Mobile.Combatant.Name);

							m_Mobile.Combatant = null;
							Action = ActionType.Guard;
							return true;
						}
					}
				}
			}

			// When we have no ammo, we flee
			Container pack = m_Mobile.Backpack;

			if ( pack == null || pack.FindItemByType( typeof( Arrow ) ) == null )
			{
				Action = ActionType.Flee;
				return true;
			}

			
			// At 20% we should check if we must leave
			if ( m_Mobile.Hits < m_Mobile.HitsMax*20/100 )
			{
				bool bFlee = false;
				// if my current hits are more than my opponent, i don't care
				if ( m_Mobile.Combatant != null && m_Mobile.Hits < m_Mobile.Combatant.Hits)
				{
					int iDiff = m_Mobile.Combatant.Hits - m_Mobile.Hits;

					if ( Utility.Random(0, 100) > 10 + iDiff) // 10% to flee + the diff of hits
					{
						bFlee = true;
					}
				}
				else if ( m_Mobile.Combatant != null && m_Mobile.Hits >= m_Mobile.Combatant.Hits)
				{
					if ( Utility.Random(0, 100) > 10 ) // 10% to flee
					{
						bFlee = true;
					}
				}
						
				if (bFlee)
				{
					Action = ActionType.Flee; 
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
	}
}