using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Guilds;
using Server.Mobiles;
using Server.Prompts;
using Server.Targeting;
using Server.Accounting;
using Server.Commands;
using Server.Commands.Generic;
using System.Collections.Generic;

namespace Server.Factions
{
	[CustomEnum( new string[]{ "Minax", "Council of Mages", "True Britannians", "Shadowlords" } )]
	public abstract class Faction : IComparable
	{
		private FactionDefinition m_Definition;
		private FactionState m_State;
		private StrongholdRegion m_StrongholdRegion;

		public StrongholdRegion StrongholdRegion
		{
			get{ return m_StrongholdRegion; }
			set{ m_StrongholdRegion = value; }
		}

		public FactionDefinition Definition
		{
			get{ return m_Definition; }
			set
			{
				m_Definition = value;
				m_StrongholdRegion = new StrongholdRegion( this );
			}
		}

		public FactionState State
		{
			get{ return m_State; }
			set{ m_State = value; }
		}

		public Election Election
		{
			get{ return m_State.Election; }
			set{ m_State.Election = value; }
		}

		public Mobile Commander
		{
			get{ return m_State.Commander; }
			set{ m_State.Commander = value; }
		}

		public int Tithe
		{
			get{ return m_State.Tithe; }
			set{ m_State.Tithe = value; }
		}

		public int Silver
		{
			get{ return m_State.Silver; }
			set{ m_State.Silver = value; }
		}

		public List<PlayerState> Members
		{
			get{ return m_State.Members; }
			set{ m_State.Members = value; }
		}

		public static readonly TimeSpan LeavePeriod = TimeSpan.FromDays( 7.0 );

		public bool FactionMessageReady
		{
			get{ return m_State.FactionMessageReady; }
		}

		public void Broadcast( string text )
		{
			Broadcast( 0x3B2, text );
		}

		public void Broadcast( int hue, string text )
		{
			List<PlayerState> members = Members;

			for ( int i = 0; i < members.Count; ++i )
				members[i].Mobile.SendMessage( hue, text );
		}

		public void Broadcast( int number )
		{
			List<PlayerState> members = Members;

			for ( int i = 0; i < members.Count; ++i )
				members[i].Mobile.SendLocalizedMessage( number );
		}

		public void Broadcast( string format, params object[] args )
		{
			Broadcast( String.Format( format, args ) );
		}

		public void Broadcast( int hue, string format, params object[] args )
		{
			Broadcast( hue, String.Format( format, args ) );
		}

		public void BeginBroadcast( Mobile from )
		{
			from.SendLocalizedMessage( 1010265 ); // Enter Faction Message
			from.Prompt = new BroadcastPrompt( this );
		}

		public void EndBroadcast( Mobile from, string text )
		{
			if ( from.AccessLevel == AccessLevel.Player )
				m_State.RegisterBroadcast();

			Broadcast( Definition.HueBroadcast, "{0} [Commander] {1} : {2}", from.Name, Definition.FriendlyName, text );
		}

		private class BroadcastPrompt : Prompt
		{
			private Faction m_Faction;

			public BroadcastPrompt( Faction faction )
			{
				m_Faction = faction;
			}

			public override void OnResponse( Mobile from, string text )
			{
				m_Faction.EndBroadcast( from, text );
			}
		}

		public void BeginHonorLeadership( Mobile from )
		{
			from.SendLocalizedMessage( 502090 ); // Click on the player whom you wish to honor.
			from.BeginTarget( 12, false, TargetFlags.None, new TargetCallback( HonorLeadership_OnTarget ) );
		}

		public void HonorLeadership_OnTarget( Mobile from, object obj )
		{
			if ( obj is Mobile )
			{
				Mobile recv = (Mobile) obj;

				PlayerState giveState = PlayerState.Find( from );
				PlayerState recvState = PlayerState.Find( recv );

				if ( giveState == null )
					return;

				if ( recvState == null || recvState.Faction != giveState.Faction )
				{
					from.SendLocalizedMessage( 1042497 ); // Only faction mates can be honored this way.
				}
				else if ( giveState.KillPoints < 5 )
				{
					from.SendLocalizedMessage( 1042499 ); // You must have at least five kill points to honor them.
				}
				else
				{
					giveState.KillPoints -= 5;
					recvState.KillPoints += 4;

					// TODO: Confirm no message sent to giver
					recv.SendLocalizedMessage( 1042500 ); // You have been honored with four kill points.
				}
			}
			else
			{
				from.SendLocalizedMessage( 1042496 ); // You may only honor another player.
			}
		}

		public virtual void AddMember( Mobile mob )
		{
			PlayerState ps = new PlayerState( mob, this, Members );
			Members.Add( ps );

			mob.AddToBackpack( FactionItem.Imbue( new Robe(), this, false, Definition.HuePrimary ) );
			mob.SendLocalizedMessage( 1010374 ); // You have been granted a robe which signifies your faction

			mob.InvalidateProperties();
			mob.Delta( MobileDelta.Noto );

			mob.FixedEffect( 0x373A, 10, 30 );
			mob.PlaySound( 0x209 );

			OnRankUpdate( ps );
		}

		public static bool IsNearType( Mobile mob, Type type, int range )
		{
			bool mobs = type.IsSubclassOf( typeof( Mobile ) );
			bool items = type.IsSubclassOf( typeof( Item ) );

			IPooledEnumerable eable;

			if ( mobs )
				eable = mob.GetMobilesInRange( range );
			else if ( items )
				eable = mob.GetItemsInRange( range );
			else
				return false;

			foreach ( object obj in eable )
			{
				if ( type.IsAssignableFrom( obj.GetType() ) )
				{
					eable.Free();
					return true;
				}
			}

			eable.Free();
			return false;
		}

		public static bool IsNearType( Mobile mob, Type[] types, int range )
		{
			/*
			bool mobs = type.IsSubclassOf( typeof( Mobile ) );
			bool items = type.IsSubclassOf( typeof( Item ) );

			IPooledEnumerable eable;

			if( mobs )
				eable = mob.GetMobilesInRange( range );
			else if( items )
				eable = mob.GetItemsInRange( range );
			else
				return false;
			 * */

			IPooledEnumerable eable = mob.GetObjectsInRange( range );

			foreach( object obj in eable )
			{
				Type objType = obj.GetType();

				for( int i = 0; i < types.Length; i++ )
				{
					if( types[i].IsAssignableFrom( objType ) )
					{
						eable.Free();
						return true;
					}
				}
			}

			eable.Free();
			return false;
		}

		public void RemovePlayerState( PlayerState pl )
		{
			if ( pl == null || !Members.Contains( pl ) )
				return;

			Members.Remove( pl );

			PlayerMobile pm = (PlayerMobile) pl.Mobile;
			if( pm == null )
				return;

			Mobile mob = pl.Mobile;
			if ( pm.FactionPlayerState == pl )
			{
				pm.FactionPlayerState = null;

				mob.InvalidateProperties();
				mob.Delta( MobileDelta.Noto );

				if ( Election.IsCandidate( mob ) )
					Election.RemoveCandidate( mob );

				if ( pl.Finance != null )
					pl.Finance.Finance = null;

				if ( pl.Sheriff != null )
					pl.Sheriff.Sheriff = null;

				Election.RemoveVoter( mob );

				if ( Commander == mob )
					Commander = null;

				pm.ValidateEquipment();
			}
		}

		public void RemoveMember( Mobile mob )
		{
			PlayerState pl = PlayerState.Find( mob );

			if ( pl == null || !Members.Contains( pl ) )
				return;

			if( mob.Backpack != null )
			{
				//Ordinarily, through normal faction removal, this will never find any sigils.
				//Only with a leave delay less than the ReturnPeriod or a Faction Kick/Ban, will this ever do anything
				Item[] sigils = mob.Backpack.FindItemsByType( typeof( Sigil ) );

				for ( int i = 0; i < sigils.Length; ++i )
					((Sigil)sigils[i]).ReturnHome();
			}

			Members.Remove( pl );

			if ( mob is PlayerMobile )
				((PlayerMobile)mob).FactionPlayerState = null;

			mob.InvalidateProperties();
			mob.Delta( MobileDelta.Noto );

			if ( Election.IsCandidate( mob ) )
				Election.RemoveCandidate( mob );

			Election.RemoveVoter( mob );

			if ( pl.Finance != null )
				pl.Finance.Finance = null;

			if ( pl.Sheriff != null )
				pl.Sheriff.Sheriff = null;

			if ( Commander == mob )
				Commander = null;

			if ( mob is PlayerMobile )
				((PlayerMobile)mob).ValidateEquipment();
		}

		public void JoinGuilded( PlayerMobile mob, Guild guild )
		{
			if ( mob.Young )
			{
				guild.RemoveMember( mob );
				mob.SendLocalizedMessage( 1042283 ); // You have been kicked out of your guild!  Young players may not remain in a guild which is allied with a faction.
			}
			else if ( AlreadyHasCharInFaction( mob ) )
			{
				guild.RemoveMember( mob );
				mob.SendLocalizedMessage( 1005281 ); // You have been kicked out of your guild due to factional overlap
			}
			else if ( IsFactionBanned( mob ) )
			{
				guild.RemoveMember( mob );
				mob.SendLocalizedMessage( 1005052 ); // You are currently banned from the faction system
			}
			else
			{
				AddMember( mob );
				mob.SendLocalizedMessage( 1042756, true, " " + m_Definition.FriendlyName ); // You are now joining a faction:
			}
		}

		public void JoinAlone( Mobile mob )
		{
			AddMember( mob );
			mob.SendLocalizedMessage( 1005058 ); // You have joined the faction
		}

		private bool AlreadyHasCharInFaction( Mobile mob )
		{
			Account acct = mob.Account as Account;

			if ( acct != null )
			{
				for ( int i = 0; i < acct.Length; ++i )
				{
					Mobile c = acct[i];

					if ( Find( c ) != null )
						return true;
				}
			}

			return false;
		}

		public static bool IsFactionBanned( Mobile mob )
		{
			Account acct = mob.Account as Account;

			if ( acct == null )
				return false;

			return ( acct.GetTag( "FactionBanned" ) != null );
		}

		public void OnJoinAccepted( Mobile mob )
		{
			PlayerMobile pm = mob as PlayerMobile;

			if ( pm == null )
				return; // sanity

			PlayerState pl = PlayerState.Find( pm );

			if ( pm.Young )
				pm.SendLocalizedMessage( 1010104 ); // You cannot join a faction as a young player
			else if ( pl != null && pl.IsLeaving )
				pm.SendLocalizedMessage( 1005051 ); // You cannot use the faction stone until you have finished quitting your current faction
			else if ( AlreadyHasCharInFaction( pm ) )
				pm.SendLocalizedMessage( 1005059 ); // You cannot join a faction because you already declared your allegiance with another character
			else if ( IsFactionBanned( mob ) )
				pm.SendLocalizedMessage( 1005052 ); // You are currently banned from the faction system
			else if ( pm.Guild != null )
			{
				Guild guild = pm.Guild as Guild;

				if ( guild.Leader != pm )
					pm.SendLocalizedMessage( 1005057 ); // You cannot join a faction because you are in a guild and not the guildmaster
				else if ( !Guild.NewGuildSystem && guild.Type != GuildType.Regular )
					pm.SendLocalizedMessage( 1042161 ); // You cannot join a faction because your guild is an Order or Chaos type.
				else if ( !Guild.NewGuildSystem && guild.Enemies != null && guild.Enemies.Count > 0 )	//CAN join w/wars in new system
					pm.SendLocalizedMessage( 1005056 ); // You cannot join a faction with active Wars	
				else if ( !CanHandleInflux( guild.Members.Count ) )
					pm.SendLocalizedMessage( 1018031 ); // In the interest of faction stability, this faction declines to accept new members for now.
				else
				{
					List<Mobile> members = new List<Mobile>( guild.Members );

					for ( int i = 0; i < members.Count; ++i )
					{
						PlayerMobile member = members[i] as PlayerMobile;

						if ( member == null )
							continue;

						JoinGuilded( member, guild );
					}
				}
			}
			else if ( !CanHandleInflux( 1 ) )
			{
				pm.SendLocalizedMessage( 1018031 ); // In the interest of faction stability, this faction declines to accept new members for now.
			}
			else
			{
				JoinAlone( mob );
			}
		}

		public bool IsCommander( Mobile mob )
		{
			if ( mob == null )
				return false;

			return ( mob.AccessLevel >= AccessLevel.GameMaster || mob == Commander );
		}

		public Faction()
		{
			m_State = new FactionState( this );
		}

		public override string ToString()
		{
			return m_Definition.FriendlyName;
		}

		public int CompareTo( object obj )
		{
			return m_Definition.Sort - ((Faction)obj).m_Definition.Sort;
		}

		public static bool CheckLeaveTimer( Mobile mob )
		{
			PlayerState pl = PlayerState.Find( mob );

			if ( pl == null || !pl.IsLeaving )
				return false;

			if ( (pl.Leaving + LeavePeriod) >= DateTime.Now )
				return false;

			mob.SendLocalizedMessage( 1005163 ); // You have now quit your faction

			pl.Faction.RemoveMember( mob );

			return true;
		}

		protected virtual void OnRankUpdate( PlayerState pl )
		{
		}

		public void UpdateRanks()
		{
			List<PlayerState> members = Members;

			List<PlayerState> list = new List<PlayerState>( members );

			list.Sort();

			RankDefinition[] ranks = m_Definition.Ranks;

			for ( int i = 0; i < list.Count; ++i )
			{
				PlayerState pl = list[i];

				int percent;

				if ( list.Count == 1 )
					percent = 1000;
				else
					percent = (i * 1000) / (list.Count - 1);

				RankDefinition rank = null;

				for ( int j = 0; j < ranks.Length; ++j )
				{
					RankDefinition check = ranks[j];

					if ( percent >= check.Required )
					{
						rank = check;
						break;
					}
				}

				if ( pl.Rank != rank )
				{
					pl.Rank = rank;
					OnRankUpdate( pl );
				}
			}
		}

		public static void Initialize()
		{
			EventSink.Login += new LoginEventHandler( EventSink_Login );
			EventSink.Logout += new LogoutEventHandler( EventSink_Logout );

			Timer.DelayCall( TimeSpan.FromSeconds( 30.0 ), TimeSpan.FromSeconds( 30.0 ), new TimerCallback( ProcessTick ) );

			CommandSystem.Register( "FactionElection", AccessLevel.GameMaster, new CommandEventHandler( FactionElection_OnCommand ) );
			CommandSystem.Register( "FactionCommander", AccessLevel.Administrator, new CommandEventHandler( FactionCommander_OnCommand ) );
			CommandSystem.Register( "FactionItemReset", AccessLevel.Administrator, new CommandEventHandler( FactionItemReset_OnCommand ) );
			CommandSystem.Register( "FactionReset", AccessLevel.Administrator, new CommandEventHandler( FactionReset_OnCommand ) );
			CommandSystem.Register( "FactionTownReset", AccessLevel.Administrator, new CommandEventHandler( FactionTownReset_OnCommand ) );
		}

		public static void FactionTownReset_OnCommand( CommandEventArgs e )
		{
			List<BaseMonolith> monoliths = BaseMonolith.Monoliths;

			for ( int i = 0; i < monoliths.Count; ++i )
				monoliths[i].Sigil = null;

			List<Town> towns = Town.Towns;

			for ( int i = 0; i < towns.Count; ++i )
			{
				towns[i].Silver = 0;
				towns[i].Sheriff = null;
				towns[i].Finance = null;
				towns[i].Tax = 0;
				towns[i].Owner = null;
			}

			List<Sigil> sigils = Sigil.Sigils;

			for ( int i = 0; i < sigils.Count; ++i )
			{
				sigils[i].Corrupted = null;
				sigils[i].Corrupting = null;
				sigils[i].LastStolen = DateTime.MinValue;
				sigils[i].GraceStart = DateTime.MinValue;
				sigils[i].CorruptionStart = DateTime.MinValue;
				sigils[i].PurificationStart = DateTime.MinValue;
				sigils[i].LastMonolith = null;
				sigils[i].ReturnHome();
			}

			List<Faction> factions = Faction.Factions;

			for ( int i = 0; i < factions.Count; ++i )
			{
				Faction f = factions[i];

				List<FactionItem> list = new List<FactionItem>( f.State.FactionItems );

				for ( int j = 0; j < list.Count; ++j )
				{
					FactionItem fi = list[j];

					if ( fi.Expiration == DateTime.MinValue )
						fi.Item.Delete();
					else
						fi.Detach();
				}
			}
		}

		public static void FactionReset_OnCommand( CommandEventArgs e )
		{
			List<BaseMonolith> monoliths = BaseMonolith.Monoliths;

			for ( int i = 0; i < monoliths.Count; ++i )
				monoliths[i].Sigil = null;

			List<Town> towns = Town.Towns;

			for ( int i = 0; i < towns.Count; ++i )
			{
				towns[i].Silver = 0;
				towns[i].Sheriff = null;
				towns[i].Finance = null;
				towns[i].Tax = 0;
				towns[i].Owner = null;
			}

			List<Sigil> sigils = Sigil.Sigils;

			for ( int i = 0; i < sigils.Count; ++i )
			{
				sigils[i].Corrupted = null;
				sigils[i].Corrupting = null;
				sigils[i].LastStolen = DateTime.MinValue;
				sigils[i].GraceStart = DateTime.MinValue;
				sigils[i].CorruptionStart = DateTime.MinValue;
				sigils[i].PurificationStart = DateTime.MinValue;
				sigils[i].LastMonolith = null;
				sigils[i].ReturnHome();
			}

			List<Faction> factions = Faction.Factions;

			for ( int i = 0; i < factions.Count; ++i )
			{
				Faction f = factions[i];

				List<PlayerState> playerStateList = new List<PlayerState>( f.Members );

				for( int j = 0; j < playerStateList.Count; ++j )
					f.RemoveMember( playerStateList[j].Mobile );

				List<FactionItem> factionItemList = new List<FactionItem>( f.State.FactionItems );

				for( int j = 0; j < factionItemList.Count; ++j )
				{
					FactionItem fi = (FactionItem)factionItemList[j];

					if ( fi.Expiration == DateTime.MinValue )
						fi.Item.Delete();
					else
						fi.Detach();
				}

				List<BaseFactionTrap> factionTrapList = new List<BaseFactionTrap>( f.Traps );

				for( int j = 0; j < factionTrapList.Count; ++j )
					factionTrapList[j].Delete();
			}
		}

		public static void FactionItemReset_OnCommand( CommandEventArgs e )
		{
			ArrayList pots = new ArrayList();

			foreach ( Item item in World.Items.Values )
			{
				if ( item is IFactionItem && !(item is HoodedShroudOfShadows) )
					pots.Add( item );
			}

			int[] hues = new int[Factions.Count * 2];

			for ( int i = 0; i < Factions.Count; ++i )
			{
				hues[0+(i*2)] = Factions[i].Definition.HuePrimary;
				hues[1+(i*2)] = Factions[i].Definition.HueSecondary;
			}

			int count = 0;

			for ( int i = 0; i < pots.Count; ++i )
			{
				Item item = (Item)pots[i];
				IFactionItem fci = (IFactionItem)item;

				if ( fci.FactionItemState != null || item.LootType != LootType.Blessed )
					continue;

				bool isHued = false;

				for ( int j = 0; j < hues.Length; ++j )
				{
					if ( item.Hue == hues[j] )
					{
						isHued = true;
						break;
					}
				}

				if ( isHued )
				{
					fci.FactionItemState = null;
					++count;
				}
			}

			e.Mobile.SendMessage( "{0} items reset", count );
		}

		public static void FactionCommander_OnCommand( CommandEventArgs e )
		{
			e.Mobile.SendMessage( "Target a player to make them the faction commander." );
			e.Mobile.BeginTarget( -1, false, TargetFlags.None, new TargetCallback( FactionCommander_OnTarget ) );
		}

		public static void FactionCommander_OnTarget( Mobile from, object obj )
		{
			if ( obj is PlayerMobile )
			{
				Mobile targ = (Mobile)obj;
				PlayerState pl = PlayerState.Find( targ );

				if ( pl != null )
				{
					pl.Faction.Commander = targ;
					from.SendMessage( "You have appointed them as the faction commander." );
				}
				else
				{
					from.SendMessage( "They are not in a faction." );
				}
			}
			else
			{
				from.SendMessage( "That is not a player." );
			}
		}

		public static void FactionElection_OnCommand( CommandEventArgs e )
		{
			e.Mobile.SendMessage( "Target a faction stone to open its election properties." );
			e.Mobile.BeginTarget( -1, false, TargetFlags.None, new TargetCallback( FactionElection_OnTarget ) );
		}

		public static void FactionElection_OnTarget( Mobile from, object obj )
		{
			if ( obj is FactionStone )
			{
				Faction faction = ((FactionStone)obj).Faction;

				if ( faction != null )
					from.SendGump( new ElectionManagementGump( faction.Election ) );
					//from.SendGump( new Gumps.PropertiesGump( from, faction.Election ) );
				else
					from.SendMessage( "That stone has no faction assigned." );
			}
			else
			{
				from.SendMessage( "That is not a faction stone." );
			}
		}

		public static void FactionKick_OnCommand( CommandEventArgs e )
		{
			e.Mobile.SendMessage( "Target a player to remove them from their faction." );
			e.Mobile.BeginTarget( -1, false, TargetFlags.None, new TargetCallback( FactionKick_OnTarget ) );
		}

		public static void FactionKick_OnTarget( Mobile from, object obj )
		{
			if ( obj is Mobile )
			{
				Mobile mob = (Mobile) obj;
				PlayerState pl = PlayerState.Find( (Mobile) mob );

				if ( pl != null )
				{
					pl.Faction.RemoveMember( mob );

					mob.SendMessage( "You have been kicked from your faction." );
					from.SendMessage( "They have been kicked from their faction." );
				}
				else
				{
					from.SendMessage( "They are not in a faction." );
				}
			}
			else
			{
				from.SendMessage( "That is not a player." );
			}
		}

		public static void ProcessTick()
		{
			List<Sigil> sigils = Sigil.Sigils;

			for ( int i = 0; i < sigils.Count; ++i )
			{
				Sigil sigil = sigils[i];

				if ( !sigil.IsBeingCorrupted && sigil.GraceStart != DateTime.MinValue && (sigil.GraceStart + Sigil.CorruptionGrace) < DateTime.Now )
				{
					if ( sigil.LastMonolith is StrongholdMonolith && ( sigil.Corrupted == null || sigil.LastMonolith.Faction != sigil.Corrupted ))
					{
						sigil.Corrupting = sigil.LastMonolith.Faction;
						sigil.CorruptionStart = DateTime.Now;
					}
					else
					{
						sigil.Corrupting = null;
						sigil.CorruptionStart = DateTime.MinValue;
					}

					sigil.GraceStart = DateTime.MinValue;
				}

				if ( sigil.LastMonolith == null || sigil.LastMonolith.Sigil == null )
				{
					if ( (sigil.LastStolen + Sigil.ReturnPeriod) < DateTime.Now )
						sigil.ReturnHome();
				}
				else
				{
					if ( sigil.IsBeingCorrupted && (sigil.CorruptionStart + Sigil.CorruptionPeriod) < DateTime.Now )
					{
						sigil.Corrupted = sigil.Corrupting;
						sigil.Corrupting = null;
						sigil.CorruptionStart = DateTime.MinValue;
						sigil.GraceStart = DateTime.MinValue;
					}
					else if ( sigil.IsPurifying && (sigil.PurificationStart + Sigil.PurificationPeriod) < DateTime.Now )
					{
						sigil.PurificationStart = DateTime.MinValue;
						sigil.Corrupted = null;
						sigil.Corrupting = null;
						sigil.CorruptionStart = DateTime.MinValue;
						sigil.GraceStart = DateTime.MinValue;
					}
				}
			}
		}

		public static void HandleDeath( Mobile mob )
		{
			HandleDeath( mob, null );
		}

		#region Skill Loss
		public const double SkillLossFactor = 1.0 / 3;
		public static readonly TimeSpan SkillLossPeriod = TimeSpan.FromMinutes( 20.0 );

		private static Hashtable m_SkillLoss = new Hashtable();

		private class SkillLossContext
		{
			public Timer m_Timer;
			public ArrayList m_Mods;
		}

		public static void ApplySkillLoss( Mobile mob )
		{
			SkillLossContext context = (SkillLossContext)m_SkillLoss[mob];

			if ( context != null )
				return;

			context = new SkillLossContext();
			m_SkillLoss[mob] = context;

			ArrayList mods = context.m_Mods = new ArrayList();

			for ( int i = 0; i < mob.Skills.Length; ++i )
			{
				Skill sk = mob.Skills[i];
				double baseValue = sk.Base;

				if ( baseValue > 0 )
				{
					SkillMod mod = new DefaultSkillMod( sk.SkillName, true, -(baseValue * SkillLossFactor) );

					mods.Add( mod );
					mob.AddSkillMod( mod );
				}
			}

			context.m_Timer = Timer.DelayCall( SkillLossPeriod, new TimerStateCallback( ClearSkillLoss_Callback ), mob );
		}

		private static void ClearSkillLoss_Callback( object state )
		{
			ClearSkillLoss( (Mobile) state );
		}

		public static bool ClearSkillLoss( Mobile mob )
		{
			SkillLossContext context = (SkillLossContext)m_SkillLoss[mob];

			if ( context == null ) {
				return false;
			}

			m_SkillLoss.Remove( mob );

			ArrayList mods = context.m_Mods;

			for ( int i = 0; i < mods.Count; ++i )
				mob.RemoveSkillMod( (SkillMod) mods[i] );

			context.m_Timer.Stop();

			return true;
		}
		#endregion

		public int AwardSilver( Mobile mob, int silver )
		{
			if ( silver <= 0 )
				return 0;

			int tithed = ( silver * Tithe ) / 100;

			Silver += tithed;

			silver = silver - tithed;

			if ( silver > 0 )
				mob.AddToBackpack( new Silver( silver ) );

			return silver;
		}

		public virtual int MaximumTraps{ get{ return 15; } }

		public List<BaseFactionTrap> Traps
		{
			get{ return m_State.Traps; }
			set{ m_State.Traps = value; }
		}

		public const int StabilityFactor = 300; // 300% greater (3 times) than smallest faction
		public const int StabilityActivation = 200; // Stablity code goes into effect when largest faction has > 200 people

		public static Faction FindSmallestFaction()
		{
			List<Faction> factions = Factions;
			Faction smallest = null;

			for ( int i = 0; i < factions.Count; ++i )
			{
				Faction faction = factions[i];

				if ( smallest == null || faction.Members.Count < smallest.Members.Count )
					smallest = faction;
			}

			return smallest;
		}

		public static bool StabilityActive()
		{
			List<Faction> factions = Factions;

			for ( int i = 0; i < factions.Count; ++i )
			{
				Faction faction = factions[i];

				if ( faction.Members.Count > StabilityActivation )
					return true;
			}

			return false;
		}

		public bool CanHandleInflux( int influx )
		{
			if( !StabilityActive())
				return true;

			Faction smallest = FindSmallestFaction();

			if ( smallest == null )
				return true; // sanity

			if ( StabilityFactor > 0 && (((this.Members.Count + influx) * 100) / StabilityFactor) > smallest.Members.Count )
				return false;

			return true;
		}

		public static void HandleDeath( Mobile victim, Mobile killer )
		{
			if ( killer == null )
				killer = victim.FindMostRecentDamager( true );

			PlayerState killerState = PlayerState.Find( killer );

			Container pack = victim.Backpack;

			if ( pack != null )
			{
				Container killerPack = ( killer == null ? null : killer.Backpack );
				Item[] sigils = pack.FindItemsByType( typeof( Sigil ) );

				for ( int i = 0; i < sigils.Length; ++i )
				{
					Sigil sigil = (Sigil)sigils[i];

					if ( killerState != null && killerPack != null )
					{
						if ( Sigil.ExistsOn( killer ) )
						{
							sigil.ReturnHome();
							killer.SendLocalizedMessage( 1010258 ); // The sigil has gone back to its home location because you already have a sigil.
						}
						else if ( !killerPack.TryDropItem( killer, sigil, false ) )
						{
							sigil.ReturnHome();
							killer.SendLocalizedMessage( 1010259 ); // The sigil has gone home because your backpack is full.
						}
					}
					else
					{
						sigil.ReturnHome();
					}
				}
			}

			if ( killerState == null )
				return;

			if ( victim is BaseCreature )
			{
				BaseCreature bc = (BaseCreature)victim;
				Faction victimFaction = bc.FactionAllegiance;

				if ( bc.Map == Faction.Facet && victimFaction != null && killerState.Faction != victimFaction )
				{
					int silver = killerState.Faction.AwardSilver( killer, bc.FactionSilverWorth );

					if ( silver > 0 )
						killer.SendLocalizedMessage( 1042748, silver.ToString( "N0" ) ); // Thou hast earned ~1_AMOUNT~ silver for vanquishing the vile creature.

				}

				#region Ethics
				if ( bc.Map == Faction.Facet && bc.GetEthicAllegiance( killer ) == BaseCreature.Allegiance.Enemy )
				{
					Ethics.Player killerEPL = Ethics.Player.Find( killer );

					if ( killerEPL != null && ( 100 - killerEPL.Power ) > Utility.Random( 100 ) )
					{
						++killerEPL.Power;
						++killerEPL.History;
					}
				}
				#endregion

				return;
			}

			PlayerState victimState = PlayerState.Find( victim );

			if ( victimState == null )
				return;

			if ( killer == victim || killerState.Faction != victimState.Faction )
				ApplySkillLoss( victim );

			if ( killerState.Faction != victimState.Faction )
			{
				if ( victimState.KillPoints <= -6 )
				{
					killer.SendLocalizedMessage( 501693 ); // This victim is not worth enough to get kill points from. 

					#region Ethics
					Ethics.Player killerEPL = Ethics.Player.Find( killer );
					Ethics.Player victimEPL = Ethics.Player.Find( victim );

					if ( killerEPL != null && victimEPL != null && victimEPL.Power > 0 && victimState.CanGiveSilverTo( killer ) )
					{
						int powerTransfer = Math.Max( 1, victimEPL.Power / 5 );

						if ( powerTransfer > ( 100 - killerEPL.Power ) )
							powerTransfer = 100 - killerEPL.Power;

						if ( powerTransfer > 0 )
						{
							victimEPL.Power -= ( powerTransfer + 1 ) / 2;
							killerEPL.Power += powerTransfer;

							killerEPL.History += powerTransfer;

							victimState.OnGivenSilverTo( killer );
						}
					}
					#endregion
				}
				else
				{
					int award = Math.Max( victimState.KillPoints / 10, 1 );

					if ( award > 40 )
						award = 40;

					if ( victimState.CanGiveSilverTo( killer ) )
					{
						if ( victimState.KillPoints > 0 )
						{
							int silver = 0;

							silver = killerState.Faction.AwardSilver( killer, award * 40 );

							if ( silver > 0 )
								killer.SendLocalizedMessage( 1042736, String.Format( "{0:N0} silver\t{1}", silver, victim.Name ) ); // You have earned ~1_SILVER_AMOUNT~ pieces for vanquishing ~2_PLAYER_NAME~!
						}

						victimState.KillPoints -= award;
						killerState.KillPoints += award;

						int offset = ( award != 1 ? 0 : 2 ); // for pluralization

						string args = String.Format( "{0}\t{1}\t{2}", award, victim.Name, killer.Name );

						killer.SendLocalizedMessage( 1042737 + offset, args ); // Thou hast been honored with ~1_KILL_POINTS~ kill point(s) for vanquishing ~2_DEAD_PLAYER~!
						victim.SendLocalizedMessage( 1042738 + offset, args ); // Thou has lost ~1_KILL_POINTS~ kill point(s) to ~3_ATTACKER_NAME~ for being vanquished!

						#region Ethics
						Ethics.Player killerEPL = Ethics.Player.Find( killer );
						Ethics.Player victimEPL = Ethics.Player.Find( victim );

						if ( killerEPL != null && victimEPL != null && victimEPL.Power > 0 )
						{
							int powerTransfer = Math.Max( 1, victimEPL.Power / 5 );

							if ( powerTransfer > ( 100 - killerEPL.Power ) )
								powerTransfer = 100 - killerEPL.Power;

							if ( powerTransfer > 0 )
							{
								victimEPL.Power -= ( powerTransfer + 1 ) / 2;
								killerEPL.Power += powerTransfer;

								killerEPL.History += powerTransfer;
							}
						}
						#endregion

						victimState.OnGivenSilverTo( killer );
					}
					else
					{
						killer.SendLocalizedMessage( 1042231 ); // You have recently defeated this enemy and thus their death brings you no honor.
					}
				}
			}
		}

		private static void EventSink_Logout( LogoutEventArgs e )
		{
			Mobile mob = e.Mobile;

			Container pack = mob.Backpack;

			if ( pack == null )
				return;

			Item[] sigils = pack.FindItemsByType( typeof( Sigil ) );

			for ( int i = 0; i < sigils.Length; ++i )
				((Sigil)sigils[i]).ReturnHome();
		}

		private static void EventSink_Login( LoginEventArgs e )
		{
			Mobile mob = e.Mobile;

			CheckLeaveTimer( mob );
		}

		public static readonly Map Facet = Map.Felucca;

		public static void WriteReference( GenericWriter writer, Faction fact )
		{
			int idx = Factions.IndexOf( fact );

			writer.WriteEncodedInt( (int) (idx + 1) );
		}

		public static List<Faction> Factions{ get{ return Reflector.Factions; } }

		public static Faction ReadReference( GenericReader reader )
		{
			int idx = reader.ReadEncodedInt() - 1;

			if ( idx >= 0 && idx < Factions.Count )
				return Factions[idx];

			return null;
		}

		public static Faction Find( Mobile mob )
		{
			return Find( mob, false, false );
		}

		public static Faction Find( Mobile mob, bool inherit )
		{
			return Find( mob, inherit, false );
		}

		public static Faction Find( Mobile mob, bool inherit, bool creatureAllegiances )
		{
			PlayerState pl = PlayerState.Find( mob );

			if ( pl != null )
				return pl.Faction;

			if ( inherit && mob is BaseCreature )
			{
				BaseCreature bc = (BaseCreature)mob;

				if ( bc.Controlled )
					return Find( bc.ControlMaster, false );
				else if ( bc.Summoned )
					return Find( bc.SummonMaster, false );
				else if ( creatureAllegiances && mob is BaseFactionGuard )
					return ((BaseFactionGuard)mob).Faction;
				else if ( creatureAllegiances )
					return bc.FactionAllegiance;
			}

			return null;
		}

		public static Faction Parse( string name )
		{
			List<Faction> factions = Factions;

			for ( int i = 0; i < factions.Count; ++i )
			{
				Faction faction = factions[i];

				if ( Insensitive.Equals( faction.Definition.FriendlyName, name ) )
					return faction;
			}

			return null;
		}
	}

	public enum FactionKickType
	{
		Kick,
		Ban,
		Unban
	}

	public class FactionKickCommand : BaseCommand
	{
		private FactionKickType m_KickType;

		public FactionKickCommand( FactionKickType kickType )
		{
			m_KickType = kickType;

			AccessLevel = AccessLevel.GameMaster;
			Supports = CommandSupport.AllMobiles;
			ObjectTypes = ObjectTypes.Mobiles;

			switch ( m_KickType )
			{
				case FactionKickType.Kick:
				{
					Commands = new string[]{ "FactionKick" };
					Usage = "FactionKick";
					Description = "Kicks the targeted player out of his current faction. This does not prevent them from rejoining.";
					break;
				}
				case FactionKickType.Ban:
				{
					Commands = new string[]{ "FactionBan" };
					Usage = "FactionBan";
					Description = "Bans the account of a targeted player from joining factions. All players on the account are removed from their current faction, if any.";
					break;
				}
				case FactionKickType.Unban:
				{
					Commands = new string[]{ "FactionUnban" };
					Usage = "FactionUnban";
					Description = "Unbans the account of a targeted player from joining factions.";
					break;
				}
			}
		}

		public override void Execute( CommandEventArgs e, object obj )
		{
			Mobile mob = (Mobile)obj;

			switch ( m_KickType )
			{
				case FactionKickType.Kick:
				{
					PlayerState pl = PlayerState.Find( mob );

					if ( pl != null )
					{
						pl.Faction.RemoveMember( mob );
						mob.SendMessage( "You have been kicked from your faction." );
						AddResponse( "They have been kicked from their faction." );
					}
					else
					{
						LogFailure( "They are not in a faction." );
					}

					break;
				}
				case FactionKickType.Ban:
				{
					Account acct = mob.Account as Account;

					if ( acct != null )
					{
						if ( acct.GetTag( "FactionBanned" ) == null )
						{
							acct.SetTag( "FactionBanned", "true" );
							AddResponse( "The account has been banned from joining factions." );
						}
						else
						{
							AddResponse( "The account is already banned from joining factions." );
						}

						for ( int i = 0; i < acct.Length; ++i )
						{
							mob = acct[i];

							if ( mob != null )
							{
								PlayerState pl = PlayerState.Find( mob );

								if ( pl != null )
								{
									pl.Faction.RemoveMember( mob );
									mob.SendMessage( "You have been kicked from your faction." );
									AddResponse( "They have been kicked from their faction." );
								}
							}
						}
					}
					else
					{
						LogFailure( "They have no assigned account." );
					}

					break;
				}
				case FactionKickType.Unban:
				{
					Account acct = mob.Account as Account;

					if ( acct != null )
					{
						if ( acct.GetTag( "FactionBanned" ) == null )
						{
							AddResponse( "The account is not already banned from joining factions." );
						}
						else
						{
							acct.RemoveTag( "FactionBanned" );
							AddResponse( "The account may now freely join factions." );
						}
					}
					else
					{
						LogFailure( "They have no assigned account." );
					}

					break;
				}
			}
		}
	}
}