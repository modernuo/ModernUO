using System;
using System.Collections.Generic;
using Server;
using Server.Engines.MLQuests.Gumps;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Mobiles;

namespace Server.Engines.MLQuests
{
	public enum ObjectiveType
	{
		All,
		Any
	}

	public class MLQuest
	{
		private bool m_Deserialized;
		private bool m_SaveEnabled;

		public bool Deserialized
		{
			get { return m_Deserialized; }
			set { m_Deserialized = value; }
		}

		public bool SaveEnabled
		{
			get { return m_SaveEnabled; }
			set { m_SaveEnabled = value; }
		}

		private bool m_Activated;
		private List<BaseObjective> m_Objectives;
		private ObjectiveType m_ObjectiveType;
		private List<BaseReward> m_Rewards;
		private List<BaseCreature> m_Questers;

		private List<MLQuestInstance> m_Instances;

		private TextDefinition m_Title;
		private TextDefinition m_Description;
		private TextDefinition m_RefuseMessage;
		private TextDefinition m_InProgressMessage;
		private TextDefinition m_CompleteMessage;

		// TODO: Flags? (Deserialized, SaveEnabled, Activated)
		private bool m_OneTimeOnly;

		public bool Activated
		{
			get { return m_Activated; }
			set { m_Activated = value; }
		}

		public List<BaseObjective> Objectives
		{
			get { return m_Objectives; }
			set { m_Objectives = value; }
		}

		public ObjectiveType ObjectiveType
		{
			get { return m_ObjectiveType; }
			set { m_ObjectiveType = value; }
		}

		public List<BaseReward> Rewards
		{
			get { return m_Rewards; }
			set { m_Rewards = value; }
		}

		public List<BaseCreature> Questers
		{
			get { return m_Questers; }
			set { m_Questers = value; }
		}

		public List<MLQuestInstance> Instances
		{
			get { return m_Instances; }
			set { m_Instances = value; }
		}

		public bool OneTimeOnly
		{
			get { return m_OneTimeOnly; }
			set { m_OneTimeOnly = value; }
		}

		public bool HasObjective<T>() where T : BaseObjective
		{
			foreach ( BaseObjective obj in m_Objectives )
			{
				if ( obj is T )
					return true;
			}

			return false;
		}

		public bool IsEscort
		{
			get { return HasObjective<EscortObjective>(); }
		}

		public bool IsSkillTrainer
		{
			get { return HasObjective<GainSkillObjective>(); }
		}

		public bool RequiresCollection
		{
			get { return HasObjective<CollectObjective>() || HasObjective<DeliverObjective>(); }
		}

		public virtual bool RecordCompletion
		{
			// TODO
			get { return ( m_OneTimeOnly /* || HasRestartDelay */ ); }
		}

		public virtual bool IsChainTriggered { get { return false; } }
		public virtual Type NextQuest { get { return null; } }

		public TextDefinition Title { get { return m_Title; } set { m_Title = value; } }
		public TextDefinition Description { get { return m_Description; } set { m_Description = value; } }
		public TextDefinition RefusalMessage { get { return m_RefuseMessage; } set { m_RefuseMessage = value; } }
		public TextDefinition InProgressMessage { get { return m_InProgressMessage; } set { m_InProgressMessage = value; } }
		public TextDefinition CompletionMessage { get { return m_CompleteMessage; } set { m_CompleteMessage = value; } }

		public MLQuest()
		{
			m_Activated = false;
			m_Objectives = new List<BaseObjective>();
			m_ObjectiveType = ObjectiveType.All;
			m_Rewards = new List<BaseReward>();

			m_Questers = new List<BaseCreature>();
			m_Instances = new List<MLQuestInstance>();

			m_SaveEnabled = true;
		}

		public void Register( BaseCreature quester )
		{
			if ( MLQuestSystem.Debug )
				Console.WriteLine( "INFO: Registering quester: {0} => {1}", quester.Serial, GetType().Name );

			m_Questers.Add( quester );
		}

		public void Unregister( BaseCreature quester )
		{
			if ( MLQuestSystem.Debug )
				Console.WriteLine( "INFO: Unregistering quester: {0} => {1}", quester.Serial, GetType().Name );

			m_Questers.Remove( quester );

			for ( int i = m_Instances.Count - 1; i >= 0; --i )
			{
				MLQuestInstance instance = m_Instances[i];

				if ( instance.Quester == quester )
					instance.OnQuesterDeleted();
			}
		}

		public virtual void Generate()
		{
			if ( MLQuestSystem.Debug )
				Console.WriteLine( "INFO: Generating quest: {0}", GetType() );
		}

		#region Generation Methods

		public void PutSpawner( Spawner s, Point3D loc, Map map )
		{
			// Auto cleanup on regeneration
			/*
			List<Item> toDelete = new List<Item>();

			foreach ( Item item in map.GetItemsInRange( loc, 0 ) )
			{
				if ( item is Spawner )
					toDelete.Add( item );
			}

			foreach ( Item item in toDelete )
				item.Delete();
			*/

			s.Name = String.Format( "MLQS-{0}", GetType().Name );
			s.MoveToWorld( loc, map );
		}

		#endregion

		public MLQuestInstance CreateInstance( BaseCreature quester, PlayerMobile pm )
		{
			return new MLQuestInstance( this, quester, pm );
		}

		public virtual bool CanOffer( BaseCreature quester, PlayerMobile pm )
		{
			if ( !m_Activated || quester.Deleted || !m_Questers.Contains( quester ) )
				return false;

			MLQuestContext context = MLQuestSystem.GetContext( pm );

			if ( context != null )
			{
				if ( context.IsFull )
				{
					MLQuestSystem.Tell( quester, pm, 1080107 ); // I'm sorry, I have nothing for you at this time.
					return false;
				}

				DateTime lastDone;

				// TODO: Also recursively check NextQuest? (This would explain Aemaeth1)
				if ( context.HasDoneQuest( this, out lastDone ) )
				{
					if ( m_OneTimeOnly )
					{
						MLQuestSystem.Tell( quester, pm, 1075454 ); // I cannot offer you the quest again.
						return false;
					}
					//else if ( m_RestartDelay > TimeSpan.Zero && lastDone + m_RestartDelay > DateTime.Now )
					//{
					//	MLQuestSystem.Tell( quester, pm, "Piss off!" );
					//	return false;
					//}
				}
			}

			// Note: On OSI this is checked before max concurrent / one time only (at least for EscortObjective),
			// but it's more intuitive for players to have it here
			foreach ( BaseObjective obj in m_Objectives )
			{
				if ( !obj.CanOffer( quester, pm ) )
					return false;
			}

			return true;
		}

		public virtual void SendOffer( BaseCreature quester, PlayerMobile pm )
		{
			pm.SendGump( new QuestOfferGump( this, quester, pm ) );
		}

		public virtual void OnAccept( BaseCreature quester, PlayerMobile pm )
		{
			if ( !CanOffer( quester, pm ) )
				return;

			MLQuestInstance instance = CreateInstance( quester, pm );

			pm.SendLocalizedMessage( 1049019 ); // You have accepted the Quest.
			pm.SendSound( 0x2E7 ); // private sound

			foreach ( BaseObjectiveInstance obj in instance.Objectives )
				obj.OnQuestAccepted();
		}

		public virtual void OnRefuse( BaseCreature quester, PlayerMobile pm )
		{
			if ( !CanOffer( quester, pm ) )
				return;

			pm.SendGump( new QuestConversationGump( this, pm, RefusalMessage ) );
		}

		// Note: Be careful with awarding stuff in OnComplete (like fame), this CAN
		// be triggered multiple times for certain objectives. Use OnRewardClaimed instead.
		public virtual void OnComplete( MLQuestInstance instance )
		{
			instance.Player.SendLocalizedMessage( 1072273, "", 0x23 ); // You've completed a quest!  Don't forget to collect your reward.

			// Note: For some quests this message is sent instead:
			//instance.Player.SendLocalizedMessage( 1073775, "", 0x23 ); // Your quest is complete. Return for your reward.
		}

		public virtual void GetRewards( MLQuestInstance instance )
		{
			instance.SendRewardGump();
		}

		public virtual void OnRewardClaimed( MLQuestInstance instance )
		{
		}

		public virtual void OnCancel( MLQuestInstance instance )
		{
		}

		public virtual void OnQuesterDeleted( MLQuestInstance instance )
		{
		}

		public virtual void OnPlayerDeath( MLQuestInstance instance )
		{
		}

		public static void Serialize( GenericWriter writer, MLQuest quest )
		{
			MLQuestSystem.WriteQuestRef( writer, quest );
			writer.Write( quest.Version );
		}

		public static void Deserialize( GenericReader reader, int version )
		{
			MLQuest quest = MLQuestSystem.ReadQuestRef( reader );
			int oldVersion = reader.ReadInt();

			if ( quest == null )
				return; // not saved or no longer exists

			quest.Refresh( oldVersion );
			quest.m_Deserialized = true;
		}

		public virtual int Version { get { return 0; } }

		public virtual void Refresh( int oldVersion )
		{
		}
	}
}
