using System;
using Server;
using Server.Engines.MLQuests;
using Server.Mobiles;
using Server.Gumps;
using System.Collections.Generic;
using Server.Misc;
using Server.Items;

namespace Server.Engines.MLQuests.Objectives
{
	public class EscortObjective : BaseObjective
	{
		private QuestArea m_Destination;

		public QuestArea Destination
		{
			get { return m_Destination; }
			set { m_Destination = value; }
		}

		public EscortObjective()
			: this( null )
		{
		}

		public EscortObjective( QuestArea destination )
		{
			m_Destination = destination;
		}

		public override bool CanOffer( IQuestGiver quester, PlayerMobile pm, bool message )
		{
			if ( ( quester is BaseCreature && ( (BaseCreature)quester ).Controlled ) || ( quester is BaseEscortable && ( (BaseEscortable)quester ).IsBeingDeleted ) )
				return false;

			MLQuestContext context = MLQuestSystem.GetContext( pm );

			if ( context != null )
			{
				foreach ( MLQuestInstance instance in context.QuestInstances )
				{
					if ( instance.Quest.IsEscort )
					{
						if ( message )
							MLQuestSystem.Tell( quester, pm, 500896 ); // I see you already have an escort.

						return false;
					}
				}
			}

			DateTime nextEscort = pm.LastEscortTime + BaseEscortable.EscortDelay;

			if ( nextEscort > DateTime.UtcNow )
			{
				if ( message )
				{
					int minutes = (int)Math.Ceiling( ( nextEscort - DateTime.UtcNow ).TotalMinutes );

					if ( minutes == 1 )
						MLQuestSystem.Tell( quester, pm, "You must rest 1 minute before we set out on this journey." );
					else
						MLQuestSystem.Tell( quester, pm, 1071195, minutes.ToString() ); // You must rest ~1_minsleft~ minutes before we set out on this journey.
				}

				return false;
			}

			return true;
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			g.AddHtmlLocalized( 98, y, 312, 16, 1072206, 0x15F90, false, false ); // Escort to

			if ( m_Destination.Name.Number > 0 )
				g.AddHtmlLocalized( 173, y, 312, 20, m_Destination.Name.Number, 0xFFFFFF, false, false );
			else if ( m_Destination.Name.String != null )
				g.AddLabel( 173, y, 0x481, m_Destination.Name.String );

			y += 16;
		}

		public override BaseObjectiveInstance CreateInstance( MLQuestInstance instance )
		{
			if ( instance == null || m_Destination == null )
				return null;

			return new EscortObjectiveInstance( this, instance );
		}
	}

	public class EscortObjectiveInstance : BaseObjectiveInstance
	{
		private EscortObjective m_Objective;
		private bool m_HasCompleted;
		private Timer m_Timer;
		private DateTime m_LastSeenEscorter;
		private BaseCreature m_Escort;

		public bool HasCompleted
		{
			get { return m_HasCompleted; }
			set { m_HasCompleted = value; }
		}

		public EscortObjectiveInstance( EscortObjective objective, MLQuestInstance instance )
			: base( instance, objective )
		{
			m_Objective = objective;
			m_HasCompleted = false;
			m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 5 ), TimeSpan.FromSeconds( 5 ), new TimerCallback( CheckDestination ) );
			m_LastSeenEscorter = DateTime.UtcNow;
			m_Escort = instance.Quester as BaseCreature;

			if ( MLQuestSystem.Debug && m_Escort == null && instance.Quester != null )
				Console.WriteLine( "Warning: EscortObjective is not supported for type '{0}'", instance.Quester.GetType().Name );
		}

		public override bool IsCompleted()
		{
			return m_HasCompleted;
		}

		private void CheckDestination()
		{
			if ( m_Escort == null || m_HasCompleted ) // Completed by deserialization
			{
				StopTimer();
				return;
			}

			MLQuestInstance instance = Instance;
			PlayerMobile pm = instance.Player;

			if ( instance.Removed )
			{
				Abandon();
			}
			else if ( m_Objective.Destination.Contains( m_Escort ) )
			{
				m_Escort.Say( 1042809, pm.Name ); // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.

				if ( pm.Young || m_Escort.Region.IsPartOf( "Haven Island" ) )
					Titles.AwardFame( pm, 10, true );
				else
					VirtueHelper.AwardVirtue( pm, VirtueName.Compassion, ( m_Escort is BaseEscortable && ( (BaseEscortable)m_Escort ).IsPrisoner ) ? 400 : 200 );

				EndFollow( m_Escort );
				StopTimer();

				m_HasCompleted = true;
				CheckComplete();

				// Auto claim reward
				MLQuestSystem.OnDoubleClick( m_Escort, pm );
			}
			else if ( pm.Map != m_Escort.Map || !pm.InRange( m_Escort, 30 ) ) // TODO: verify range
			{
				if ( m_LastSeenEscorter + BaseEscortable.AbandonDelay <= DateTime.UtcNow )
					Abandon();
			}
			else
			{
				m_LastSeenEscorter = DateTime.UtcNow;
			}
		}

		private void StopTimer()
		{
			if ( m_Timer != null )
			{
				m_Timer.Stop();
				m_Timer = null;
			}
		}

		public static void BeginFollow( BaseCreature quester, PlayerMobile pm )
		{
			quester.ControlSlots = 0;
			quester.SetControlMaster( pm );

			quester.ActiveSpeed = 0.1;
			quester.PassiveSpeed = 0.2;

			quester.ControlOrder = OrderType.Follow;
			quester.ControlTarget = pm;

			quester.CantWalk = false;
			quester.CurrentSpeed = 0.1;
		}

		public static void EndFollow( BaseCreature quester )
		{
			quester.ActiveSpeed = 0.2;
			quester.PassiveSpeed = 1.0;

			quester.ControlOrder = OrderType.None;
			quester.ControlTarget = null;

			quester.CurrentSpeed = 1.0;

			quester.SetControlMaster( null );

			if ( quester is BaseEscortable )
				( (BaseEscortable)quester ).BeginDelete();
		}

		public override void OnQuestAccepted()
		{
			MLQuestInstance instance = Instance;
			PlayerMobile pm = instance.Player;

			pm.LastEscortTime = DateTime.UtcNow;

			if ( m_Escort != null )
				BeginFollow( m_Escort, pm );
		}

		public void Abandon()
		{
			StopTimer();

			MLQuestInstance instance = Instance;
			PlayerMobile pm = instance.Player;

			if ( m_Escort != null && !m_Escort.Deleted )
			{
				if ( !pm.Alive )
					m_Escort.Say( 500901 ); // Ack!  My escort has come to haunt me!
				else
					m_Escort.Say( 500902 ); // My escort seems to have abandoned me!

				EndFollow( m_Escort );
			}

			// Note: this sound is sent twice on OSI (once here and once in Cancel())
			//m_Player.SendSound( 0x5B3 ); // private sound
			pm.SendLocalizedMessage( 1071194 ); // You have failed your escort quest...

			if ( !instance.Removed )
				instance.Cancel();
		}

		public override void OnQuesterDeleted()
		{
			if ( IsCompleted() || Instance.Removed )
				return;

			Abandon();
		}

		public override void OnPlayerDeath()
		{
			// Note: OSI also cancels it when the quest is already complete
			if ( /*IsCompleted() ||*/ Instance.Removed )
				return;

			Instance.Cancel();
		}

		public override void OnExpire()
		{
			Abandon();
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			m_Objective.WriteToGump( g, ref y );

			base.WriteToGump( g, ref y );

			// No extra instance stuff printed for this objective
		}

		public override DataType ExtraDataType { get { return DataType.EscortObjective; } }

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( m_HasCompleted );
		}
	}
}
