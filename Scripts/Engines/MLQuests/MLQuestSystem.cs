using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Engines.MLQuests.Definitions;
using Server.Engines.MLQuests.Gumps;
using Server.Engines.MLQuests.Objectives;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System.Collections;
using Server.Commands.Generic;
using Server.Items;
using Server.Targeting;
using System.IO;

namespace Server.Engines.MLQuests
{
	public static class MLQuestSystem
	{
		public static bool Enabled { get { return Core.ML; } }

		public const int MaxConcurrentQuests = 10;
		public const int SpeechColor = 0x3B2;

		public static readonly bool AutoGenerateNew = true;
		public static readonly bool Debug = false;

		private static Dictionary<Type, MLQuest> m_Quests;
		private static Dictionary<Type, List<MLQuest>> m_QuestGivers;
		private static Dictionary<PlayerMobile, MLQuestContext> m_Contexts;

		public static readonly List<MLQuest> EmptyList = new List<MLQuest>();

		public static Dictionary<Type, MLQuest> Quests
		{
			get { return m_Quests; }
		}

		public static Dictionary<Type, List<MLQuest>> QuestGivers
		{
			get { return m_QuestGivers; }
		}

		public static Dictionary<PlayerMobile, MLQuestContext> Contexts
		{
			get { return m_Contexts; }
		}

		static MLQuestSystem()
		{
			m_Quests = new Dictionary<Type, MLQuest>();
			m_QuestGivers = new Dictionary<Type, List<MLQuest>>();
			m_Contexts = new Dictionary<PlayerMobile, MLQuestContext>();

			string cfgPath = Path.Combine( Core.BaseDirectory, Path.Combine( "Data", "MLQuests.cfg" ) );

			Type baseQuestType = typeof( MLQuest );
			Type baseQuesterType = typeof( IQuestGiver );

			if ( File.Exists( cfgPath ) )
			{
				using ( StreamReader sr = new StreamReader( cfgPath ) )
				{
					string line;

					while ( ( line = sr.ReadLine() ) != null )
					{
						if ( line.Length == 0 || line.StartsWith( "#" ) )
							continue;

						string[] split = line.Split( '\t' );

						Type type = ScriptCompiler.FindTypeByName( split[0] );

						if ( type == null || !baseQuestType.IsAssignableFrom( type ) )
						{
							if ( Debug )
								Console.WriteLine( "Warning: {1} quest type '{0}'", split[0], ( type == null ) ? "Unknown" : "Invalid" );

							continue;
						}

						MLQuest quest = null;

						try
						{
							quest = Activator.CreateInstance( type ) as MLQuest;
						}
						catch { }

						if ( quest == null )
							continue;

						Register( type, quest );

						for ( int i = 1; i < split.Length; ++i )
						{
							Type questerType = ScriptCompiler.FindTypeByName( split[i] );

							if ( questerType == null || !baseQuesterType.IsAssignableFrom( questerType ) )
							{
								if ( Debug )
									Console.WriteLine( "Warning: {1} quester type '{0}'", split[i], ( questerType == null ) ? "Unknown" : "Invalid" );

								continue;
							}

							RegisterQuestGiver( quest, questerType );
						}
					}
				}
			}
		}

		private static void Register( Type type, MLQuest quest )
		{
			m_Quests[type] = quest;
		}

		private static void RegisterQuestGiver( MLQuest quest, Type questerType )
		{
			List<MLQuest> questList;

			if ( !m_QuestGivers.TryGetValue( questerType, out questList ) )
				m_QuestGivers[questerType] = questList = new List<MLQuest>();

			questList.Add( quest );
		}

		public static void Register( MLQuest quest, params Type[] questerTypes )
		{
			Register( quest.GetType(), quest );

			foreach ( Type questerType in questerTypes )
				RegisterQuestGiver( quest, questerType );
		}

		public static void Initialize()
		{
			if ( !Enabled )
				return;

			if ( AutoGenerateNew )
			{
				foreach ( MLQuest quest in m_Quests.Values )
				{
					if ( quest != null && !quest.Deserialized )
						quest.Generate();
				}
			}

			MLQuestPersistence.EnsureExistence();

			CommandSystem.Register( "MLQuestsInfo", AccessLevel.Administrator, new CommandEventHandler( MLQuestsInfo_OnCommand ) );
			CommandSystem.Register( "SaveQuest", AccessLevel.Administrator, new CommandEventHandler( SaveQuest_OnCommand ) );
			CommandSystem.Register( "SaveAllQuests", AccessLevel.Administrator, new CommandEventHandler( SaveAllQuests_OnCommand ) );
			CommandSystem.Register( "InvalidQuestItems", AccessLevel.Administrator, new CommandEventHandler( InvalidQuestItems_OnCommand ) );

			TargetCommands.Register( new ViewQuestsCommand() );
			TargetCommands.Register( new ViewContextCommand() );

			EventSink.QuestGumpRequest += new QuestGumpRequestHandler( EventSink_QuestGumpRequest );
		}

		[Usage( "MLQuestsInfo" )]
		[Description( "Displays general information about the ML quest system, or a quest by type name." )]
		public static void MLQuestsInfo_OnCommand( CommandEventArgs e )
		{
			Mobile m = e.Mobile;

			if ( e.Length == 0 )
			{
				m.SendMessage( "Quest table length: {0}", m_Quests.Count );
				return;
			}

			Type index = ScriptCompiler.FindTypeByName( e.GetString( 0 ) );
			MLQuest quest;

			if ( index == null || !m_Quests.TryGetValue( index, out quest ) )
			{
				m.SendMessage( "Invalid quest type name." );
				return;
			}

			m.SendMessage( "Activated: {0}", quest.Activated );
			m.SendMessage( "Number of objectives: {0}", quest.Objectives.Count );
			m.SendMessage( "Objective type: {0}", quest.ObjectiveType );
			m.SendMessage( "Number of active instances: {0}", quest.Instances.Count );
		}

		public class ViewQuestsCommand : BaseCommand
		{
			public ViewQuestsCommand()
			{
				AccessLevel = AccessLevel.GameMaster;
				Supports = CommandSupport.Simple;
				Commands = new string[] { "ViewQuests" };
				ObjectTypes = ObjectTypes.Mobiles;
				Usage = "ViewQuests";
				Description = "Displays a targeted mobile's quest overview.";
			}

			public override void Execute( CommandEventArgs e, object obj )
			{
				Mobile from = e.Mobile;
				PlayerMobile pm = obj as PlayerMobile;

				if ( pm == null )
				{
					LogFailure( "That is not a player." );
					return;
				}

				CommandLogging.WriteLine( from, "{0} {1} viewing quest overview of {2}", from.AccessLevel, CommandLogging.Format( from ), CommandLogging.Format( pm ) );
				from.SendGump( new QuestLogGump( pm, false ) );
			}
		}

		private class ViewContextCommand : BaseCommand
		{
			public ViewContextCommand()
			{
				AccessLevel = AccessLevel.GameMaster;
				Supports = CommandSupport.Simple;
				Commands = new string[] { "ViewMLContext" };
				ObjectTypes = ObjectTypes.Mobiles;
				Usage = "ViewMLContext";
				Description = "Opens the ML quest context for a targeted mobile.";
			}

			public override void Execute( CommandEventArgs e, object obj )
			{
				PlayerMobile pm = obj as PlayerMobile;

				if ( pm == null )
					LogFailure( "They have no ML quest context." );
				else
					e.Mobile.SendGump( new PropertiesGump( e.Mobile, GetOrCreateContext( pm ) ) );
			}
		}

		[Usage( "SaveQuest <type> [saveEnabled=true]" )]
		[Description( "Allows serialization for a specific quest to be turned on or off." )]
		public static void SaveQuest_OnCommand( CommandEventArgs e )
		{
			Mobile m = e.Mobile;

			if ( e.Length == 0 || e.Length > 2 )
			{
				m.SendMessage( "Syntax: SaveQuest <id> [saveEnabled=true]" );
				return;
			}

			Type index = ScriptCompiler.FindTypeByName( e.GetString( 0 ) );
			MLQuest quest;

			if ( index == null || !m_Quests.TryGetValue( index, out quest ) )
			{
				m.SendMessage( "Invalid quest type name." );
				return;
			}

			bool enable = ( e.Length == 2 ) ? e.GetBoolean( 1 ) : true;

			quest.SaveEnabled = enable;
			m.SendMessage( "Serialization for quest {0} is now {1}.", quest.GetType().Name, enable ? "enabled" : "disabled" );

			if ( AutoGenerateNew && !enable )
				m.SendMessage( "Please note that automatic generation of new quests is ON. This quest will be regenerated on the next server start." );
		}

		[Usage( "SaveAllQuests [saveEnabled=true]" )]
		[Description( "Allows serialization for all quests to be turned on or off." )]
		public static void SaveAllQuests_OnCommand( CommandEventArgs e )
		{
			Mobile m = e.Mobile;

			if ( e.Length > 1 )
			{
				m.SendMessage( "Syntax: SaveAllQuests [saveEnabled=true]" );
				return;
			}

			bool enable = ( e.Length == 1 ) ? e.GetBoolean( 0 ) : true;

			foreach ( MLQuest quest in m_Quests.Values )
				quest.SaveEnabled = enable;

			m.SendMessage( "Serialization for all quests is now {0}.", enable ? "enabled" : "disabled" );

			if ( AutoGenerateNew && !enable )
				m.SendMessage( "Please note that automatic generation of new quests is ON. All quests will be regenerated on the next server start." );
		}

		[Usage( "InvalidQuestItems" )]
		[Description( "Provides an overview of all quest items not located in the top-level of a player's backpack." )]
		public static void InvalidQuestItems_OnCommand( CommandEventArgs e )
		{
			Mobile m = e.Mobile;

			ArrayList found = new ArrayList();

			foreach ( Item item in World.Items.Values )
			{
				if ( item.QuestItem )
				{
					Backpack pack = item.Parent as Backpack;

					if ( pack != null )
					{
						PlayerMobile player = pack.Parent as PlayerMobile;

						if ( player != null && player.Backpack == pack )
							continue;
					}

					found.Add( item );
				}
			}

			if ( found.Count == 0 )
				m.SendMessage( "No matching objects found." );
			else
				m.SendGump( new InterfaceGump( m, new string[] { "Object" }, found, 0, null ) );
		}

		private static bool FindQuest( IQuestGiver quester, PlayerMobile pm, MLQuestContext context, out MLQuest quest, out MLQuestInstance entry )
		{
			quest = null;
			entry = null;

			List<MLQuest> quests = quester.MLQuests;
			Type questerType = quester.GetType();

			// 1. Check quests in progress with this NPC (overriding deliveries is intended)
			if ( context != null )
			{
				foreach ( MLQuest questEntry in quests )
				{
					MLQuestInstance instance = context.FindInstance( questEntry );

					if ( instance != null && ( instance.Quester == quester || ( !questEntry.IsEscort && instance.QuesterType == questerType ) ) )
					{
						entry = instance;
						quest = questEntry;
						return true;
					}
				}
			}

			// 2. Check deliveries (overriding chain offers is intended)
			if ( ( entry = HandleDelivery( pm, quester, questerType ) ) != null )
			{
				quest = entry.Quest;
				return true;
			}

			// 3. Check chain quest offers
			if ( context != null )
			{
				foreach ( MLQuest questEntry in quests )
				{
					if ( questEntry.IsChainTriggered && context.ChainOffers.Contains( questEntry ) )
					{
						quest = questEntry;
						return true;
					}
				}
			}

			// 4. Random quest
			quest = RandomStarterQuest( quester, pm, context );

			return ( quest != null );
		}

		public static void OnDoubleClick( IQuestGiver quester, PlayerMobile pm )
		{
			if ( quester.Deleted || !pm.Alive )
				return;

			MLQuestContext context = GetContext( pm );

			MLQuest quest;
			MLQuestInstance entry;

			if ( !FindQuest( quester, pm, context, out quest, out entry ) )
			{
				Tell( quester, pm, 1080107 ); // I'm sorry, I have nothing for you at this time.
				return;
			}

			if ( entry != null )
			{
				TurnToFace( quester, pm );

				if ( entry.Failed )
					return; // Note: OSI sends no gump at all for failed quests, they have to be cancelled in the quest overview
				else if ( entry.ClaimReward )
					entry.SendRewardOffer();
				else if ( entry.IsCompleted() )
					entry.SendReportBackGump();
				else
					entry.SendProgressGump();
			}
			else if ( quest.CanOffer( quester, pm, context, true ) )
			{
				TurnToFace( quester, pm );

				quest.SendOffer( quester, pm );
			}
		}

		public static bool CanMarkQuestItem( PlayerMobile pm, Item item, Type type )
		{
			MLQuestContext context = GetContext( pm );

			if ( context != null )
			{
				foreach ( MLQuestInstance quest in context.QuestInstances )
				{
					if ( !quest.ClaimReward && quest.AllowsQuestItem( item, type ) )
						return true;
				}
			}

			return false;
		}

		private static void OnMarkQuestItem( PlayerMobile pm, Item item, Type type )
		{
			MLQuestContext context = GetContext( pm );

			if ( context == null )
				return;

			List<MLQuestInstance> instances = context.QuestInstances;

			// We don't foreach because CheckComplete() can potentially modify the MLQuests list
			for ( int i = instances.Count - 1; i >= 0; --i )
			{
				MLQuestInstance instance = instances[i];

				if ( instance.ClaimReward )
					continue;

				foreach ( BaseObjectiveInstance objective in instance.Objectives )
				{
					if ( !objective.Expired && objective.AllowsQuestItem( item, type ) )
					{
						objective.CheckComplete(); // yes, this can happen multiple times (for multiple quests)
						break;
					}
				}
			}
		}

		public static bool MarkQuestItem( PlayerMobile pm, Item item )
		{
			Type type = item.GetType();

			if ( CanMarkQuestItem( pm, item, type ) )
			{
				item.QuestItem = true;
				OnMarkQuestItem( pm, item, type );

				return true;
			}

			return false;
		}

		public static void HandleSkillGain( PlayerMobile pm, SkillName skill )
		{
			MLQuestContext context = GetContext( pm );

			if ( context == null )
				return;

			List<MLQuestInstance> instances = context.QuestInstances;

			for ( int i = instances.Count - 1; i >= 0; --i )
			{
				MLQuestInstance instance = instances[i];

				if ( instance.ClaimReward )
					continue;

				foreach ( BaseObjectiveInstance objective in instance.Objectives )
				{
					if ( !objective.Expired && objective is GainSkillObjectiveInstance && ( (GainSkillObjectiveInstance)objective ).Handles( skill ) )
					{
						objective.CheckComplete();
						break;
					}
				}
			}
		}

		public static void HandleKill( PlayerMobile pm, Mobile mob )
		{
			MLQuestContext context = GetContext( pm );

			if ( context == null )
				return;

			List<MLQuestInstance> instances = context.QuestInstances;

			Type type = null;

			for ( int i = instances.Count - 1; i >= 0; --i )
			{
				MLQuestInstance instance = instances[i];

				if ( instance.ClaimReward )
					continue;

				/* A kill only counts for a single objective within a quest,
				 * but it can count for multiple quests. This is something not
				 * currently observable on OSI, so it is assumed behavior.
				 */
				foreach ( BaseObjectiveInstance objective in instance.Objectives )
				{
					if ( !objective.Expired && objective is KillObjectiveInstance )
					{
						KillObjectiveInstance kill = (KillObjectiveInstance)objective;

						if ( type == null )
							type = mob.GetType();

						if ( kill.AddKill( mob, type ) )
						{
							kill.CheckComplete();
							break;
						}
					}
				}
			}
		}

		public static MLQuestInstance HandleDelivery( PlayerMobile pm, IQuestGiver quester, Type questerType )
		{
			MLQuestContext context = GetContext( pm );

			if ( context == null )
				return null;

			List<MLQuestInstance> instances = context.QuestInstances;
			MLQuestInstance deliverInstance = null;

			for ( int i = instances.Count - 1; i >= 0; --i )
			{
				MLQuestInstance instance = instances[i];

				// Do NOT skip quests on ClaimReward, because the quester still needs the quest ref!
				//if ( instance.ClaimReward )
				//	continue;

				foreach ( BaseObjectiveInstance objective in instance.Objectives )
				{
					// Note: On OSI, expired deliveries can still be completed. Bug?
					if ( !objective.Expired && objective is DeliverObjectiveInstance )
					{
						DeliverObjectiveInstance deliver = (DeliverObjectiveInstance)objective;

						if ( deliver.IsDestination( quester, questerType ) )
						{
							if ( !deliver.HasCompleted ) // objective completes only once
							{
								deliver.HasCompleted = true;
								deliver.CheckComplete();

								// The quest is continued with this NPC (important for chains)
								instance.Quester = quester;
							}

							if ( deliverInstance == null )
								deliverInstance = instance;

							break; // don't return, we may have to complete more deliveries
						}
					}
				}
			}

			return deliverInstance;
		}

		public static MLQuestContext GetContext( PlayerMobile pm )
		{
			MLQuestContext context;
			m_Contexts.TryGetValue( pm, out context );

			return context;
		}

		public static MLQuestContext GetOrCreateContext( PlayerMobile pm )
		{
			MLQuestContext context;

			if ( !m_Contexts.TryGetValue( pm, out context ) )
				m_Contexts[pm] = context = new MLQuestContext( pm );

			return context;
		}

		public static void HandleDeath( PlayerMobile pm )
		{
			MLQuestContext context = GetContext( pm );

			if ( context != null )
				context.HandleDeath();
		}

		public static void HandleDeletion( PlayerMobile pm )
		{
			MLQuestContext context = GetContext( pm );

			if ( context != null )
			{
				context.HandleDeletion();
				m_Contexts.Remove( pm );
			}
		}

		public static void HandleDeletion( IQuestGiver quester )
		{
			foreach ( MLQuest quest in quester.MLQuests )
			{
				List<MLQuestInstance> instances = quest.Instances;

				for ( int i = instances.Count - 1; i >= 0; --i )
				{
					MLQuestInstance instance = instances[i];

					if ( instance.Quester == quester )
						instance.OnQuesterDeleted();
				}
			}
		}

		public static void EventSink_QuestGumpRequest( QuestGumpRequestArgs args )
		{
			PlayerMobile pm = args.Mobile as PlayerMobile;

			if ( !Enabled || pm == null )
				return;

			pm.SendGump( new QuestLogGump( pm ) );
		}

		private static List<MLQuest> m_EligiblePool = new List<MLQuest>();

		public static MLQuest RandomStarterQuest( IQuestGiver quester, PlayerMobile pm, MLQuestContext context )
		{
			List<MLQuest> quests = quester.MLQuests;

			if ( quests.Count == 0 )
				return null;

			m_EligiblePool.Clear();
			MLQuest fallback = null;

			foreach ( MLQuest quest in quests )
			{
				if ( quest.IsChainTriggered || ( context != null && context.IsDoingQuest( quest ) ) )
					continue;

				/*
				 * Save first quest that reaches the CanOffer call.
				 * If no quests are valid at all, return this quest for displaying the CanOffer error message.
				 */
				if ( fallback == null )
					fallback = quest;

				if ( quest.CanOffer( quester, pm, context, false ) )
					m_EligiblePool.Add( quest );
			}

			if ( m_EligiblePool.Count == 0 )
				return fallback;

			return m_EligiblePool[Utility.Random( m_EligiblePool.Count )];
		}

		public static void TurnToFace( IQuestGiver quester, Mobile mob )
		{
			if ( quester is Mobile )
			{
				Mobile m = (Mobile)quester;
				m.Direction = m.GetDirectionTo( mob );
			}
		}

		public static void Tell( IQuestGiver quester, PlayerMobile pm, int cliloc )
		{
			TurnToFace( quester, pm );

			if ( quester is Mobile )
				((Mobile)quester).PrivateOverheadMessage( MessageType.Regular, SpeechColor, cliloc, pm.NetState );
			else if ( quester is Item )
				MessageHelper.SendLocalizedMessageTo( (Item)quester, pm, cliloc, SpeechColor );
			else
				pm.SendLocalizedMessage( cliloc, "", SpeechColor );
		}

		public static void Tell( IQuestGiver quester, PlayerMobile pm, int cliloc, string args )
		{
			TurnToFace( quester, pm );

			if ( quester is Mobile )
				((Mobile)quester).PrivateOverheadMessage( MessageType.Regular, SpeechColor, cliloc, args, pm.NetState );
			else if ( quester is Item )
				MessageHelper.SendLocalizedMessageTo( (Item)quester, pm, cliloc, args, SpeechColor );
			else
				pm.SendLocalizedMessage( cliloc, args, SpeechColor );
		}

		public static void Tell( IQuestGiver quester, PlayerMobile pm, string message )
		{
			TurnToFace( quester, pm );

			if ( quester is Mobile )
				((Mobile)quester).PrivateOverheadMessage( MessageType.Regular, SpeechColor, false, message, pm.NetState );
			else if ( quester is Item )
				MessageHelper.SendMessageTo( (Item)quester, pm, message, SpeechColor );
			else
				pm.SendMessage( SpeechColor, message );
		}

		public static void TellDef( IQuestGiver quester, PlayerMobile pm, TextDefinition def )
		{
			if ( def == null )
				return;

			if ( def.Number > 0 )
				Tell( quester, pm, def.Number );
			else if ( def.String != null )
				Tell( quester, pm, def.String );
		}

		public static void WriteQuestRef( GenericWriter writer, MLQuest quest )
		{
			writer.Write( ( quest != null && quest.SaveEnabled ) ? quest.GetType().FullName : null );
		}

		public static MLQuest ReadQuestRef( GenericReader reader )
		{
			string typeName = reader.ReadString();

			if ( typeName == null )
				return null; // not serialized

			Type questType = ScriptCompiler.FindTypeByFullName( typeName );

			if ( questType == null )
				return null; // no longer a type

			return FindQuest( questType );
		}

		public static MLQuest FindQuest( Type questType )
		{
			MLQuest result;
			m_Quests.TryGetValue( questType, out result );

			return result;
		}

		public static List<MLQuest> FindQuestList( Type questerType )
		{
			List<MLQuest> result;

			if ( m_QuestGivers.TryGetValue( questerType, out result ) )
				return result;

			return EmptyList;
		}
	}
}
