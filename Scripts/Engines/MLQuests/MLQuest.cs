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

		private List<MLQuestInstance> m_Instances;

		private TextDefinition m_Title;
		private TextDefinition m_Description;
		private TextDefinition m_RefuseMessage;
		private TextDefinition m_InProgressMessage;
		private TextDefinition m_CompletionMessage;
		private TextDefinition m_CompletionNotice;

		// TODO: Flags? (Deserialized, SaveEnabled, Activated)
		private bool m_OneTimeOnly;
		private bool m_HasRestartDelay;

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

		public bool HasRestartDelay
		{
			get { return m_HasRestartDelay; }
			set { m_HasRestartDelay = value; }
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
			get { return ( m_OneTimeOnly || m_HasRestartDelay ); }
		}

		public virtual bool IsChainTriggered { get { return false; } }
		public virtual Type NextQuest { get { return null; } }

		public TextDefinition Title { get { return m_Title; } set { m_Title = value; } }
		public TextDefinition Description { get { return m_Description; } set { m_Description = value; } }
		public TextDefinition RefusalMessage { get { return m_RefuseMessage; } set { m_RefuseMessage = value; } }
		public TextDefinition InProgressMessage { get { return m_InProgressMessage; } set { m_InProgressMessage = value; } }
		public TextDefinition CompletionMessage { get { return m_CompletionMessage; } set { m_CompletionMessage = value; } }
		public TextDefinition CompletionNotice { get { return m_CompletionNotice; } set { m_CompletionNotice = value; } }

		public static readonly TextDefinition CompletionNoticeDefault = new TextDefinition( 1072273 ); // You've completed a quest!  Don't forget to collect your reward.
		public static readonly TextDefinition CompletionNoticeShort = new TextDefinition( 1046258 ); // Your quest is complete.
		public static readonly TextDefinition CompletionNoticeShortReturn = new TextDefinition( 1073775 ); // Your quest is complete. Return for your reward.
		public static readonly TextDefinition CompletionNoticeCraft = new TextDefinition( 1073967 ); // You obtained what you seek, now receive your reward.

		public MLQuest()
		{
			m_Activated = false;
			m_Objectives = new List<BaseObjective>();
			m_ObjectiveType = ObjectiveType.All;
			m_Rewards = new List<BaseReward>();
			m_CompletionNotice = CompletionNoticeDefault;

			m_Instances = new List<MLQuestInstance>();

			m_SaveEnabled = true;
		}

		public virtual void Generate()
		{
			if ( MLQuestSystem.Debug )
				Console.WriteLine( "INFO: Generating quest: {0}", GetType() );
		}

		#region Generation Methods

		public void PutSpawner( Spawner s, Point3D loc, Map map )
		{
			string name = String.Format( "MLQS-{0}", GetType().Name );

			// Auto cleanup on regeneration
			List<Item> toDelete = new List<Item>();

			foreach ( Item item in map.GetItemsInRange( loc, 0 ) )
			{
				if ( item is Spawner && item.Name == name )
					toDelete.Add( item );
			}

			foreach ( Item item in toDelete )
				item.Delete();

			s.Name = name;
			s.MoveToWorld( loc, map );
		}

		public void PutDeco( Item deco, Point3D loc, Map map )
		{
			// Auto cleanup on regeneration
			List<Item> toDelete = new List<Item>();

			foreach ( Item item in map.GetItemsInRange( loc, 0 ) )
			{
				if ( item.ItemID == deco.ItemID && item.Z == loc.Z )
					toDelete.Add( item );
			}

			foreach ( Item item in toDelete )
				item.Delete();

			deco.MoveToWorld( loc, map );
		}

		#endregion

		public MLQuestInstance CreateInstance( IQuestGiver quester, PlayerMobile pm )
		{
			return new MLQuestInstance( this, quester, pm );
		}

		public bool CanOffer( IQuestGiver quester, PlayerMobile pm, bool message )
		{
			return CanOffer( quester, pm, MLQuestSystem.GetContext( pm ), message );
		}

		public virtual bool CanOffer( IQuestGiver quester, PlayerMobile pm, MLQuestContext context, bool message )
		{
			if ( !m_Activated || quester.Deleted )
				return false;

			if ( context != null )
			{
				if ( context.IsFull )
				{
					if ( message )
						MLQuestSystem.Tell( quester, pm, 1080107 ); // I'm sorry, I have nothing for you at this time.

					return false;
				}

				MLQuest checkQuest = this;

				while ( checkQuest != null )
				{
					DateTime nextAvailable;

					if ( context.HasDoneQuest( checkQuest, out nextAvailable ) )
					{
						if ( checkQuest.OneTimeOnly )
						{
							if ( message )
								MLQuestSystem.Tell( quester, pm, 1075454 ); // I cannot offer you the quest again.

							return false;
						}
						else if ( nextAvailable > DateTime.UtcNow )
						{
							if ( message )
								MLQuestSystem.Tell( quester, pm, 1075575 ); // I'm sorry, but I don't have anything else for you right now. Could you check back with me in a few minutes?

							return false;
						}
					}

					if ( checkQuest.NextQuest == null )
						break;

					checkQuest = MLQuestSystem.FindQuest( checkQuest.NextQuest );
				}
			}

			foreach ( BaseObjective obj in m_Objectives )
			{
				if ( !obj.CanOffer( quester, pm, message ) )
					return false;
			}

			return true;
		}

		public virtual void SendOffer( IQuestGiver quester, PlayerMobile pm )
		{
			pm.SendGump( new QuestOfferGump( this, quester, pm ) );
		}

		public virtual void OnAccept( IQuestGiver quester, PlayerMobile pm )
		{
			if ( !CanOffer( quester, pm, true ) )
				return;

			MLQuestInstance instance = CreateInstance( quester, pm );

			pm.SendLocalizedMessage( 1049019 ); // You have accepted the Quest.
			pm.SendSound( 0x2E7 ); // private sound

			OnAccepted( instance );

			foreach ( BaseObjectiveInstance obj in instance.Objectives )
				obj.OnQuestAccepted();
		}

		public virtual void OnAccepted( MLQuestInstance instance )
		{
		}

		public virtual void OnRefuse( IQuestGiver quester, PlayerMobile pm )
		{
			pm.SendGump( new QuestConversationGump( this, pm, RefusalMessage ) );
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

		public virtual TimeSpan GetRestartDelay()
		{
			return TimeSpan.FromSeconds( Utility.Random( 1, 5 ) * 30 );
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
