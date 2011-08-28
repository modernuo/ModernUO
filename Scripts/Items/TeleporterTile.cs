using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
	public class TeleporterTile : Item, ISecurable
	{
		[CommandProperty( AccessLevel.GameMaster )]
		public SecureLevel Level { get { return m_Level; } set { m_Level = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public TeleporterTile Dest { get { return m_Dest; } set { m_Dest = value; } }

		public virtual BaseHouse House
		{
			get
			{
				if( m_House == null || m_House.Deleted )
				{
					m_House = FindHouse( this );
				}

				return m_House;
			}
		}

		public override int LabelNumber { get { return ( Dest == null || Dest.Deleted ) ? 1114916 : 1113857; } }

		public override bool HandlesOnMovement { get { return true; } }

		private SecureLevel m_Level;
		private TeleporterTile m_Dest;
		private BaseHouse m_House;

		private Dictionary<PlayerMobile, Timer> m_InSequence;

		public enum TelMes
		{
			None = -1,
			UnlinkedName = 1114916,		// house teleporter (unlinked)	1114916
			LinkedName = 1113857,		// house teleporter	1113857
			SelectTele = 1114918,		// Select a House Teleporter to link to.	1114918
			MustBeInPack = 1114917,		// This must be in your backpack to link it.	1114917
			TelesNowLinked = 1114919,		// The two House Teleporters are now linked.	1114919
			MustBeSecured = 502692,		// This must be in a house and be locked down to work.	502692
			NotAllowed = 1019004,		// You are not allowed to travel there.	1019004
			InvalidDest = 1113858,		// This teleporter does not have a valid destination.	1113858
			Criminal = 1005270,		// Thou'rt a criminal and cannot escape so easily...	1005270
			InCombat = 1005564,		// Wouldst thou flee during the heat of battle??	100556
			NoAccess = 1061637						// You are not allowed to access this.	1061637
		}

		[Constructable]
		public TeleporterTile()
			: base( 0x40bb )
		{
		}

		public override void OnAdded( object parent )
		{
			Movable = true;
			m_InSequence = new Dictionary<PlayerMobile, Timer>();
			base.OnAdded( parent );
		}

		public TeleporterTile( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int)0 );

			writer.Write( (Item)m_Dest );
			writer.Write( (int)m_Level );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			m_Dest = (TeleporterTile)reader.ReadItem();
			m_Level = (SecureLevel)reader.ReadInt();

			if( m_InSequence == null )
			{
				m_InSequence = new Dictionary<PlayerMobile, Timer>();
			}
		}

		public override void OnLocationChange( Point3D oldLocation )
		{
			base.OnLocationChange( oldLocation );

			bool tmp = ( ( m_House = null ) == House );
		}

		public override void OnMapChange()
		{
			base.OnMapChange();

			bool tmp = ( ( m_House = null ) == House );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if( RootParent == from )
			{
				from.SendLocalizedMessage( 1114918 );

				from.Target = new InternalTarget( this );
			}
			else
			{
				from.SendLocalizedMessage( 1114917 );
			}

			base.OnDoubleClick( from );
		}

		public virtual bool IsValidObject( object obj, TelMes message, ref TelMes error )
		{
			if( obj != null )
			{
				if( obj is Item )
				{
					Item tmp = obj as Item;

					return ( ( ( tmp != null && !tmp.Deleted ) ? TelMes.None : message ) == TelMes.None );
				}

				if( obj is PlayerMobile )
				{
					PlayerMobile tmp = obj as PlayerMobile;

					return ( ( ( tmp != null && !tmp.Deleted ) ? TelMes.None : message ) == TelMes.None );
				}
			}
			return false;
		}

		private static PlayerMobile ExtractPlayerMobile( Mobile m, ref PlayerMobile player )
		{
			return player = ( m != null && !m.Deleted && m is PlayerMobile ) ? m as PlayerMobile : null;
		}

		public override bool OnMoveOver( Mobile m )
		{
			TelMes error = TelMes.None; PlayerMobile player = null;

			if( IsValidObject( ExtractPlayerMobile( m, ref player ), TelMes.None, ref error ) )
			{
				if( IsValidObject( m_Dest, TelMes.InvalidDest, ref error ) )
				{
					if( IsValidObject( House, TelMes.MustBeSecured, ref error ) )
					{
						if( IsValidPlacement( this, ref error ) )
						{
							m_InSequence[ player ] = TryReadAndCreateKey( player, null );

							if( m_InSequence[ player ] == null )
							{
								m_InSequence[ player ] = StartSequence( SequenceStart_Callback, player, 0 );
							}
						}
					}
				}
			}

			if( error == TelMes.None )
			{
				m.SendLocalizedMessage( ( (int)error ) );
			}

			return base.OnMoveOver( m );
		}

		private static bool IsValidPlacement( TeleporterTile tile, ref TelMes error )
		{
			return ( error = ( CheckSecure( tile ) || CheckSecure( tile.Dest ) ) ? TelMes.MustBeSecured : TelMes.None ) == TelMes.MustBeSecured;
		}

		private static bool CheckSecure( TeleporterTile tile )
		{
			return ( tile.IsLockedDown || tile.IsSecure );
		}

		private static BaseHouse FindHouse( TeleporterTile teleporter )
		{
			return BaseHouse.FindHouseAt( teleporter );
		}

		private static bool IsPlayerAllowed( TeleporterTile tile, PlayerMobile player, ref TelMes error )
		{
			if( CheckAccess( tile, player ) && ( CheckAccess( tile.Dest, player ) ) )
			{
				if( player.Kills < 5 || tile.Dest.Map == Map.Felucca )
				{
					if( !SpellHelper.CheckCombat( player ) )
					{
						if( player.Criminal )
						{
							error = TelMes.Criminal;
						}
					}
					else
					{
						error = TelMes.InCombat;
					}
				}
				error = TelMes.NotAllowed;
			}
			error = TelMes.NoAccess;

			return ( error == 0 );
		}

		private static bool CheckAccess( TeleporterTile teleporter, PlayerMobile player )
		{
			return ( BaseHouse.CheckAccessible( player, teleporter ) && ( teleporter.House.HasAccess( player ) ) );
		}

		public virtual Timer TryReadAndCreateKey( PlayerMobile player, Timer timer )
		{
			if( m_InSequence == null )
			{
				m_InSequence = new Dictionary<PlayerMobile, Timer>();
			}

			if( m_InSequence.Count < 1 || !m_InSequence.ContainsKey( player ) )
			{
				m_InSequence.Add( player, null );
			}

			return ( m_InSequence[ player ] != null ) ? m_InSequence[ player ] : null;
		}

		public delegate void Sequence_Callback<PlayerMobile>( PlayerMobile player );

		public virtual Timer StartSequence( Sequence_Callback<PlayerMobile> callback, PlayerMobile player, double delay )
		{
			return m_InSequence[ player ] = Timer.DelayCall<PlayerMobile>( TimeSpan.FromSeconds( delay ), new TimerStateCallback<PlayerMobile>( callback ), player );
		}

		public virtual void SequenceStart_Callback( PlayerMobile player )
		{
			if( IsStillValid( player ) )
			{
				Effects.PlaySound( player.Location, player.Map, 0x1F0 );

				StartSequence( SourceEffect_Callback, player, 0 );
			}
		}

		public virtual void SourceEffect_Callback( PlayerMobile player )
		{
			if( IsStillValid( player ) )
			{
				if( !player.Hidden || player.AccessLevel == AccessLevel.Player )
				{
					Effects.SendLocationEffect( player.Location, player.Map, 0x3728, 10, 10 );
				}

				StartSequence( Teleport_Callback, player, .5 );
			}
		}

		public virtual void Teleport_Callback( PlayerMobile player )
		{
			StartSequence( SequenceEnd_Callback, player, .5 );
		}

		public virtual void SequenceEnd_Callback( PlayerMobile player )
		{
			if( IsStillValid( player ) )
			{
				Effects.PlaySound( player.Location, player.Map, 0x1F0 );

				StartSequence( SourceEffect_Callback, player, .25 );
			}
		}

		public virtual bool IsStillValid( PlayerMobile player )
		{
			return ( ( player != null && !player.Deleted ) && player.X == X && player.Y == Y );
		}
	}

	public class InternalTarget : Target
	{
		private TeleporterTile m_Clicked;

		public InternalTarget( TeleporterTile usedtele )
			: base( 4, false, TargetFlags.None )
		{
			m_Clicked = usedtele;
		}

		protected override void OnTarget( Mobile from, object targeted )
		{
			TeleporterTile teleporter = ( targeted is TeleporterTile ) ? targeted as TeleporterTile : null;

			if( teleporter != null )
			{
				if( teleporter.RootParent != from )
				{
					from.SendLocalizedMessage( 1114917 );
				}
			}

			m_Clicked.Dest = teleporter;
			teleporter.Dest = m_Clicked;

			from.SendLocalizedMessage( 1114919 );
		}
	}
}