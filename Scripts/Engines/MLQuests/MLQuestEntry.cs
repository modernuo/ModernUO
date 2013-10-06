using System;
using System.Collections.Generic;
using Server;
using Server.Engines.MLQuests.Objectives;
using Server.Mobiles;
using Server.Network;
using Server.Engines.MLQuests.Gumps;
using Server.Engines.MLQuests.Rewards;

namespace Server.Engines.MLQuests
{
	[Flags]
	public enum MLQuestInstanceFlags : byte
	{
		None		= 0x00,
		ClaimReward	= 0x01,
		Removed		= 0x02,
		Failed		= 0x04
	}

	public class MLQuestInstance
	{
		private MLQuest m_Quest;

		private IQuestGiver m_Quester;
		private Type m_QuesterType;
		private PlayerMobile m_Player;

		private DateTime m_Accepted;
		private MLQuestInstanceFlags m_Flags;

		private BaseObjectiveInstance[] m_ObjectiveInstances;

		private Timer m_Timer;

		public MLQuestInstance( MLQuest quest, IQuestGiver quester, PlayerMobile player )
		{
			m_Quest = quest;

			m_Quester = quester;
			m_QuesterType = ( quester == null ) ? null : quester.GetType();
			m_Player = player;

			m_Accepted = DateTime.UtcNow;
			m_Flags = MLQuestInstanceFlags.None;

			m_ObjectiveInstances = new BaseObjectiveInstance[quest.Objectives.Count];

			BaseObjectiveInstance obj;
			bool timed = false;

			for ( int i = 0; i < quest.Objectives.Count; ++i )
			{
				m_ObjectiveInstances[i] = obj = quest.Objectives[i].CreateInstance( this );

				if ( obj.IsTimed )
					timed = true;
			}

			Register();

			if ( timed )
				m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 5 ), TimeSpan.FromSeconds( 5 ), Slice );
		}

		private void Register()
		{
			if ( m_Quest != null && m_Quest.Instances != null )
				m_Quest.Instances.Add( this );

			if ( m_Player != null )
				PlayerContext.QuestInstances.Add( this );
		}

		private void Unregister()
		{
			if ( m_Quest != null && m_Quest.Instances != null )
				m_Quest.Instances.Remove( this );

			if ( m_Player != null )
				PlayerContext.QuestInstances.Remove( this );

			Removed = true;
		}

		public MLQuest Quest
		{
			get { return m_Quest; }
			set { m_Quest = value; }
		}

		public IQuestGiver Quester
		{
			get { return m_Quester; }
			set
			{
				m_Quester = value;
				m_QuesterType = ( value == null ) ? null : value.GetType();
			}
		}

		public Type QuesterType
		{
			get { return m_QuesterType; }
		}

		public PlayerMobile Player
		{
			get { return m_Player; }
			set { m_Player = value; }
		}

		public MLQuestContext PlayerContext
		{
			get { return MLQuestSystem.GetOrCreateContext( m_Player ); }
		}

		public DateTime Accepted
		{
			get { return m_Accepted; }
			set { m_Accepted = value; }
		}

		public bool ClaimReward
		{
			get { return GetFlag( MLQuestInstanceFlags.ClaimReward ); }
			set { SetFlag( MLQuestInstanceFlags.ClaimReward, value ); }
		}

		public bool Removed
		{
			get { return GetFlag( MLQuestInstanceFlags.Removed ); }
			set { SetFlag( MLQuestInstanceFlags.Removed, value ); }
		}

		public bool Failed
		{
			get { return GetFlag( MLQuestInstanceFlags.Failed ); }
			set { SetFlag( MLQuestInstanceFlags.Failed, value ); }
		}

		public BaseObjectiveInstance[] Objectives
		{
			get { return m_ObjectiveInstances; }
			set { m_ObjectiveInstances = value; }
		}

		public bool AllowsQuestItem( Item item, Type type )
		{
			foreach ( BaseObjectiveInstance objective in m_ObjectiveInstances )
			{
				if ( !objective.Expired && objective.AllowsQuestItem( item, type ) )
					return true;
			}

			return false;
		}

		public bool IsCompleted()
		{
			bool requiresAll = ( m_Quest.ObjectiveType == ObjectiveType.All );

			foreach ( BaseObjectiveInstance obj in m_ObjectiveInstances )
			{
				bool complete = obj.IsCompleted();

				if ( complete && !requiresAll )
					return true;
				else if ( !complete && requiresAll )
					return false;
			}

			return requiresAll;
		}

		public void CheckComplete()
		{
			if ( IsCompleted() )
			{
				m_Player.PlaySound( 0x5B5 ); // public sound

				foreach ( BaseObjectiveInstance obj in m_ObjectiveInstances )
					obj.OnQuestCompleted();

				TextDefinition.SendMessageTo( m_Player, m_Quest.CompletionNotice, 0x23 );

				/*
				 * Advance to the ClaimReward=true stage if this quest has no
				 * completion message to show anyway. This suppresses further
				 * triggers of CheckComplete.
				 *
				 * For quests that require collections, this is done later when
				 * the player double clicks the quester.
				 */
				if ( !Removed && SkipReportBack && !m_Quest.RequiresCollection ) // An OnQuestCompleted can potentially have removed this instance already
					ContinueReportBack( false );
			}
		}

		public void Fail()
		{
			Failed = true;
		}

		private void Slice()
		{
			if ( ClaimReward || Removed )
			{
				StopTimer();
				return;
			}

			bool hasAnyFails = false;
			bool hasAnyLeft = false;

			foreach ( BaseObjectiveInstance obj in m_ObjectiveInstances )
			{
				if ( !obj.Expired )
				{
					if ( obj.IsTimed && obj.EndTime <= DateTime.UtcNow )
					{
						m_Player.SendLocalizedMessage( 1072258 ); // You failed to complete an objective in time!

						obj.Expired = true;
						obj.OnExpire();

						hasAnyFails = true;
					}
					else
					{
						hasAnyLeft = true;
					}
				}
			}

			if ( ( m_Quest.ObjectiveType == ObjectiveType.All && hasAnyFails ) || !hasAnyLeft )
				Fail();

			if ( !hasAnyLeft )
				StopTimer();
		}

		public void SendProgressGump()
		{
			m_Player.SendGump( new QuestConversationGump( m_Quest, m_Player, m_Quest.InProgressMessage ) );
		}

		public void SendRewardOffer()
		{
			m_Quest.GetRewards( this );
		}

		// TODO: Split next quest stuff from SendRewardGump stuff?
		public void SendRewardGump()
		{
			Type nextQuestType = m_Quest.NextQuest;

			if ( nextQuestType != null )
			{
				ClaimRewards(); // skip reward gump

				if ( Removed ) // rewards were claimed successfully
				{
					MLQuest nextQuest = MLQuestSystem.FindQuest( nextQuestType );

					if ( nextQuest != null )
						nextQuest.SendOffer( m_Quester, m_Player );
				}
			}
			else
			{
				m_Player.SendGump( new QuestRewardGump( this ) );
			}
		}

		public bool SkipReportBack
		{
			get { return TextDefinition.IsNullOrEmpty( m_Quest.CompletionMessage ); }
		}

		public void SendReportBackGump()
		{
			if ( SkipReportBack )
				ContinueReportBack( true ); // skip ahead
			else
				m_Player.SendGump( new QuestReportBackGump( this ) );
		}

		public void ContinueReportBack( bool sendRewardGump )
		{
			// There is a backpack check here on OSI for the rewards as well (even though it's not needed...)

			if ( m_Quest.ObjectiveType == ObjectiveType.All )
			{
				// TODO: 1115877 - You no longer have the required items to complete this quest.
				foreach ( BaseObjectiveInstance objective in m_ObjectiveInstances )
				{
					if ( !objective.IsCompleted() )
						return;
				}

				foreach ( BaseObjectiveInstance objective in m_ObjectiveInstances )
				{
					if ( !objective.OnBeforeClaimReward() )
						return;
				}

				foreach ( BaseObjectiveInstance objective in m_ObjectiveInstances )
					objective.OnClaimReward();
			}
			else
			{
				/* The following behavior is unverified, as OSI (currently) has no collect quest requiring
				 * only one objective to be completed. It is assumed that only one objective is claimed
				 * (the first completed one), even when multiple are complete.
				 */
				bool complete = false;

				foreach ( BaseObjectiveInstance objective in m_ObjectiveInstances )
				{
					if ( objective.IsCompleted() )
					{
						if ( objective.OnBeforeClaimReward() )
						{
							complete = true;
							objective.OnClaimReward();
						}

						break;
					}
				}

				if ( !complete )
					return;
			}

			ClaimReward = true;

			if ( m_Quest.HasRestartDelay )
				PlayerContext.SetDoneQuest( m_Quest, DateTime.UtcNow + m_Quest.GetRestartDelay() );

			// This is correct for ObjectiveType.Any as well
			foreach ( BaseObjectiveInstance objective in m_ObjectiveInstances )
				objective.OnAfterClaimReward();

			if ( sendRewardGump )
				SendRewardOffer();
		}

		public void ClaimRewards()
		{
			if ( m_Quest == null || m_Player == null || m_Player.Deleted || !ClaimReward || Removed )
				return;

			List<Item> rewards = new List<Item>();

			foreach ( BaseReward reward in m_Quest.Rewards )
				reward.AddRewardItems( m_Player, rewards );

			if ( rewards.Count != 0 )
			{
				// On OSI a more naive method of checking is used.
				// For containers, only the actual container item counts.
				bool canFit = true;

				foreach ( Item rewardItem in rewards )
				{
					if ( !m_Player.AddToBackpack( rewardItem ) )
					{
						canFit = false;
						break;
					}
				}

				if ( !canFit )
				{
					foreach ( Item rewardItem in rewards )
						rewardItem.Delete();

					m_Player.SendLocalizedMessage( 1078524 ); // Your backpack is full. You cannot complete the quest and receive your reward.
					return;
				}

				foreach ( Item rewardItem in rewards )
				{
					string rewardName = ( rewardItem.Name != null ) ? rewardItem.Name : String.Concat( "#", rewardItem.LabelNumber );

					if ( rewardItem.Stackable )
						m_Player.SendLocalizedMessage( 1115917, String.Concat( rewardItem.Amount, "\t", rewardName ) ); // You receive a reward: ~1_QUANTITY~ ~2_ITEM~
					else
						m_Player.SendLocalizedMessage( 1074360, rewardName ); // You receive a reward: ~1_REWARD~
				}
			}

			foreach ( BaseObjectiveInstance objective in m_ObjectiveInstances )
				objective.OnRewardClaimed();

			m_Quest.OnRewardClaimed( this );

			MLQuestContext context = PlayerContext;

			if ( m_Quest.RecordCompletion && !m_Quest.HasRestartDelay ) // Quests with restart delays are logged earlier as per OSI
				context.SetDoneQuest( m_Quest );

			if ( m_Quest.IsChainTriggered )
				context.ChainOffers.Remove( m_Quest );

			Type nextQuestType = m_Quest.NextQuest;

			if ( nextQuestType != null )
			{
				MLQuest nextQuest = MLQuestSystem.FindQuest( nextQuestType );

				if ( nextQuest != null && !context.ChainOffers.Contains( nextQuest ) )
					context.ChainOffers.Add( nextQuest );
			}

			Remove();
		}

		public void Cancel()
		{
			Cancel( false );
		}

		public void Cancel( bool removeChain )
		{
			Remove();

			m_Player.SendSound( 0x5B3 ); // private sound

			foreach ( BaseObjectiveInstance obj in m_ObjectiveInstances )
				obj.OnQuestCancelled();

			m_Quest.OnCancel( this );

			if ( removeChain )
				PlayerContext.ChainOffers.Remove( m_Quest );
		}

		public void Remove()
		{
			Unregister();
			StopTimer();
		}

		private void StopTimer()
		{
			if ( m_Timer != null )
			{
				m_Timer.Stop();
				m_Timer = null;
			}
		}

		public void OnQuesterDeleted()
		{
			foreach ( BaseObjectiveInstance obj in m_ObjectiveInstances )
				obj.OnQuesterDeleted();

			m_Quest.OnQuesterDeleted( this );
		}

		public void OnPlayerDeath()
		{
			foreach ( BaseObjectiveInstance obj in m_ObjectiveInstances )
				obj.OnPlayerDeath();

			m_Quest.OnPlayerDeath( this );
		}

		private bool GetFlag( MLQuestInstanceFlags flag )
		{
			return ( ( m_Flags & flag ) != 0 );
		}

		private void SetFlag( MLQuestInstanceFlags flag, bool value )
		{
			if ( value )
				m_Flags |= flag;
			else
				m_Flags &= ~flag;
		}

		public void Serialize( GenericWriter writer )
		{
			// Version info is written in MLQuestPersistence.Serialize

			MLQuestSystem.WriteQuestRef( writer, m_Quest );

			if ( m_Quester == null || m_Quester.Deleted )
				writer.Write( Serial.MinusOne );
			else
				writer.Write( m_Quester.Serial );

			writer.Write( ClaimReward );
			writer.Write( m_ObjectiveInstances.Length );

			foreach ( BaseObjectiveInstance objInstance in m_ObjectiveInstances )
				objInstance.Serialize( writer );
		}

		public static MLQuestInstance Deserialize( GenericReader reader, int version, PlayerMobile pm )
		{
			MLQuest quest = MLQuestSystem.ReadQuestRef( reader );

			// TODO: Serialize quester TYPE too, the quest giver reference then becomes optional (only for escorts)
			IQuestGiver quester = World.FindEntity( reader.ReadInt() ) as IQuestGiver;

			bool claimReward = reader.ReadBool();
			int objectives = reader.ReadInt();

			MLQuestInstance instance;

			if ( quest != null && quester != null && pm != null )
			{
				instance = quest.CreateInstance( quester, pm );
				instance.ClaimReward = claimReward;
			}
			else
			{
				instance = null;
			}

			for ( int i = 0; i < objectives; ++i )
				BaseObjectiveInstance.Deserialize( reader, version, ( instance != null && i < instance.Objectives.Length ) ? instance.Objectives[i] : null );

			if ( instance != null )
				instance.Slice();

			return instance;
		}
	}
}
