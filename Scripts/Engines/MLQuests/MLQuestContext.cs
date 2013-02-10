using System;
using System.Collections.Generic;
using System.Text;
using Server.Mobiles;
using Server.Engines.MLQuests.Objectives;

namespace Server.Engines.MLQuests
{
	[Flags]
	public enum MLQuestFlag
	{
		None			= 0x00,
		Spellweaving	= 0x01,
		SummonFey		= 0x02,
		SummonFiend		= 0x04,
		BedlamAccess	= 0x08
	}

	[PropertyObject]
	public class MLQuestContext
	{
		private class MLDoneQuestInfo
		{
			public MLQuest m_Quest;
			public DateTime m_NextAvailable;

			public MLDoneQuestInfo( MLQuest quest, DateTime nextAvailable )
			{
				m_Quest = quest;
				m_NextAvailable = nextAvailable;
			}

			public void Serialize( GenericWriter writer )
			{
				MLQuestSystem.WriteQuestRef( writer, m_Quest );
				writer.Write( m_NextAvailable );
			}

			public static MLDoneQuestInfo Deserialize( GenericReader reader, int version )
			{
				MLQuest quest = MLQuestSystem.ReadQuestRef( reader );
				DateTime nextAvailable = reader.ReadDateTime();

				if ( quest == null || !quest.RecordCompletion )
					return null; // forget about this record

				return new MLDoneQuestInfo( quest, nextAvailable );
			}
		}

		private PlayerMobile m_Owner;
		private List<MLQuestInstance> m_QuestInstances;
		private List<MLDoneQuestInfo> m_DoneQuests;
		private List<MLQuest> m_ChainOffers;
		private MLQuestFlag m_Flags;

		public PlayerMobile Owner
		{
			get { return m_Owner; }
		}

		public List<MLQuestInstance> QuestInstances
		{
			get { return m_QuestInstances; }
		}

		public List<MLQuest> ChainOffers
		{
			get { return m_ChainOffers; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsFull
		{
			get { return m_QuestInstances.Count >= MLQuestSystem.MaxConcurrentQuests; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Spellweaving
		{
			get { return GetFlag( MLQuestFlag.Spellweaving ); }
			set { SetFlag( MLQuestFlag.Spellweaving, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool SummonFey
		{
			get { return GetFlag( MLQuestFlag.SummonFey ); }
			set { SetFlag( MLQuestFlag.SummonFey, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool SummonFiend
		{
			get { return GetFlag( MLQuestFlag.SummonFiend ); }
			set { SetFlag( MLQuestFlag.SummonFiend, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool BedlamAccess
		{
			get { return GetFlag( MLQuestFlag.BedlamAccess ); }
			set { SetFlag( MLQuestFlag.BedlamAccess, value ); }
		}

		public MLQuestContext( PlayerMobile owner )
		{
			m_Owner = owner;
			m_QuestInstances = new List<MLQuestInstance>();
			m_DoneQuests = new List<MLDoneQuestInfo>();
			m_ChainOffers = new List<MLQuest>();
			m_Flags = MLQuestFlag.None;
		}

		public bool HasDoneQuest( Type questType )
		{
			MLQuest quest = MLQuestSystem.FindQuest( questType );

			return ( quest != null && HasDoneQuest( quest ) );
		}

		public bool HasDoneQuest( MLQuest quest )
		{
			foreach ( MLDoneQuestInfo info in m_DoneQuests )
			{
				if ( info.m_Quest == quest )
					return true;
			}

			return false;
		}

		public bool HasDoneQuest( MLQuest quest, out DateTime nextAvailable )
		{
			nextAvailable = DateTime.MinValue;

			foreach ( MLDoneQuestInfo info in m_DoneQuests )
			{
				if ( info.m_Quest == quest )
				{
					nextAvailable = info.m_NextAvailable;
					return true;
				}
			}

			return false;
		}

		public void SetDoneQuest( MLQuest quest )
		{
			SetDoneQuest( quest, DateTime.MinValue );
		}

		public void SetDoneQuest( MLQuest quest, DateTime nextAvailable )
		{
			foreach ( MLDoneQuestInfo info in m_DoneQuests )
			{
				if ( info.m_Quest == quest )
				{
					info.m_NextAvailable = nextAvailable;
					return;
				}
			}

			m_DoneQuests.Add( new MLDoneQuestInfo( quest, nextAvailable ) );
		}

		public void RemoveDoneQuest( MLQuest quest )
		{
			for ( int i = m_DoneQuests.Count - 1; i >= 0; --i )
			{
				MLDoneQuestInfo info = m_DoneQuests[i];

				if ( info.m_Quest == quest )
					m_DoneQuests.RemoveAt( i );
			}
		}

		public void HandleDeath()
		{
			for ( int i = m_QuestInstances.Count - 1; i >= 0; --i )
				m_QuestInstances[i].OnPlayerDeath();
		}

		public void HandleDeletion()
		{
			for ( int i = m_QuestInstances.Count - 1; i >= 0; --i )
				m_QuestInstances[i].Remove();
		}

		public MLQuestInstance FindInstance( Type questType )
		{
			MLQuest quest = MLQuestSystem.FindQuest( questType );

			if ( quest == null )
				return null;

			return FindInstance( quest );
		}

		public MLQuestInstance FindInstance( MLQuest quest )
		{
			foreach ( MLQuestInstance instance in m_QuestInstances )
			{
				if ( instance.Quest == quest )
					return instance;
			}

			return null;
		}

		public bool IsDoingQuest( Type questType )
		{
			MLQuest quest = MLQuestSystem.FindQuest( questType );

			return ( quest != null && IsDoingQuest( quest ) );
		}

		public bool IsDoingQuest( MLQuest quest )
		{
			return ( FindInstance( quest ) != null );
		}

		public void Serialize( GenericWriter writer )
		{
			// Version info is written in MLQuestPersistence.Serialize

			writer.WriteMobile<PlayerMobile>( m_Owner );
			writer.Write( m_QuestInstances.Count );

			foreach ( MLQuestInstance instance in m_QuestInstances )
				instance.Serialize( writer );

			writer.Write( m_DoneQuests.Count );

			foreach ( MLDoneQuestInfo info in m_DoneQuests )
				info.Serialize( writer );

			writer.Write( m_ChainOffers.Count );

			foreach ( MLQuest quest in m_ChainOffers )
				MLQuestSystem.WriteQuestRef( writer, quest );

			writer.WriteEncodedInt( (int)m_Flags );
		}

		public MLQuestContext( GenericReader reader, int version )
		{
			m_Owner = reader.ReadMobile<PlayerMobile>();
			m_QuestInstances = new List<MLQuestInstance>();
			m_DoneQuests = new List<MLDoneQuestInfo>();
			m_ChainOffers = new List<MLQuest>();

			int instances = reader.ReadInt();

			for ( int i = 0; i < instances; ++i )
			{
				MLQuestInstance instance = MLQuestInstance.Deserialize( reader, version, m_Owner );

				if ( instance != null )
					m_QuestInstances.Add( instance );
			}

			int doneQuests = reader.ReadInt();

			for ( int i = 0; i < doneQuests; ++i )
			{
				MLDoneQuestInfo info = MLDoneQuestInfo.Deserialize( reader, version );

				if ( info != null )
					m_DoneQuests.Add( info );
			}

			int chainOffers = reader.ReadInt();

			for ( int i = 0; i < chainOffers; ++i )
			{
				MLQuest quest = MLQuestSystem.ReadQuestRef( reader );

				if ( quest != null && quest.IsChainTriggered )
					m_ChainOffers.Add( quest );
			}

			m_Flags = (MLQuestFlag)reader.ReadEncodedInt();
		}

		public bool GetFlag( MLQuestFlag flag )
		{
			return ( ( m_Flags & flag ) != 0 );
		}

		public void SetFlag( MLQuestFlag flag, bool value )
		{
			if ( value )
				m_Flags |= flag;
			else
				m_Flags &= ~flag;
		}
	}
}
