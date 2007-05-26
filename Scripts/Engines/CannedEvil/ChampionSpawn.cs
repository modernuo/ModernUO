using System;
using System.Collections;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using System.Collections.Generic;

namespace Server.Engines.CannedEvil
{
	public class ChampionSpawn : Item
	{
		private bool m_Active;
		private bool m_RandomizeType;
		private ChampionSpawnType m_Type;
		private List<Mobile> m_Creatures;
		private List<Item> m_RedSkulls;
		private List<Item> m_WhiteSkulls;
		private ChampionPlatform m_Platform;
		private ChampionAltar m_Altar;
		private int m_Kills;
		private Mobile m_Champion;

		//private int m_SpawnRange;
		private Rectangle2D m_SpawnArea;
		private ChampionSpawnRegion m_Region;

		private TimeSpan m_ExpireDelay;
		private DateTime m_ExpireTime;

		private TimeSpan m_RestartDelay;
		private DateTime m_RestartTime;

		private Timer m_Timer, m_RestartTimer;

		private IdolOfTheChampion m_Idol;

		private bool m_HasBeenAdvanced;
		private bool m_ConfinedRoaming;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool ConfinedRoaming
		{
			get { return m_ConfinedRoaming; }
			set { m_ConfinedRoaming = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool HasBeenAdvanced
		{
			get { return m_HasBeenAdvanced; }
			set { m_HasBeenAdvanced = value; }
		}

		[Constructable]
		public ChampionSpawn() : base( 0xBD2 )
		{
			Movable = false;
			Visible = false;

			m_Creatures = new List<Mobile>();
			m_RedSkulls = new List<Item>();
			m_WhiteSkulls = new List<Item>();

			m_Platform = new ChampionPlatform( this );
			m_Altar = new ChampionAltar( this );
			m_Idol = new IdolOfTheChampion( this );

			m_ExpireDelay = TimeSpan.FromMinutes( 10.0 );
			m_RestartDelay = TimeSpan.FromMinutes( 5.0 );

			Timer.DelayCall( TimeSpan.Zero, new TimerCallback( SetInitialSpawnArea ) );
		}

		public void SetInitialSpawnArea()
		{
			//Previous default used to be 24;
			SpawnArea = new Rectangle2D( new Point2D( X - 24, Y - 24 ), new Point2D( X + 24, Y + 24 ) );
		}

		public void UpdateRegion()
		{
			if( m_Region != null )
				m_Region.Unregister();

			if( !Deleted && this.Map != Map.Internal )
			{
				m_Region = new ChampionSpawnRegion( this );
				m_Region.Register();
			}

			/*
			if( m_Region == null )
			{
				m_Region = new ChampionSpawnRegion( this );
			}
			else
			{
				m_Region.Unregister();
				//Why doesn't Region allow me to set it's map/Area meself? ><
				m_Region = new ChampionSpawnRegion( this );
			}
			*/
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool RandomizeType
		{
			get { return m_RandomizeType; }
			set { m_RandomizeType = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Kills
		{
			get
			{
				return m_Kills;
			}
			set
			{
				m_Kills = value;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Rectangle2D SpawnArea
		{
			get
			{
				return m_SpawnArea;
			}
			set
			{
				m_SpawnArea = value;
				InvalidateProperties();
				UpdateRegion();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan RestartDelay
		{
			get
			{
				return m_RestartDelay;
			}
			set
			{
				m_RestartDelay = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime RestartTime
		{
			get
			{
				return m_RestartTime;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan ExpireDelay
		{
			get
			{
				return m_ExpireDelay;
			}
			set
			{
				m_ExpireDelay = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime ExpireTime
		{
			get
			{
				return m_ExpireTime;
			}
			set
			{
				m_ExpireTime = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public ChampionSpawnType Type
		{
			get
			{
				return m_Type;
			}
			set
			{
				m_Type = value;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Active
		{
			get
			{
				return m_Active;
			}
			set
			{
				if( value )
					Start();
				else
					Stop();

				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Champion
		{
			get
			{
				return m_Champion;
			}
			set
			{
				m_Champion = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Level
		{
			get
			{
				return m_RedSkulls.Count;
			}
			set
			{
				for( int i = m_RedSkulls.Count - 1; i >= value; --i )
				{
					m_RedSkulls[i].Delete();
					m_RedSkulls.RemoveAt( i );
				}

				for( int i = m_RedSkulls.Count; i < value; ++i )
				{
					Item skull = new Item( 0x1854 );

					skull.Hue = 0x26;
					skull.Movable = false;
					skull.Light = LightType.Circle150;

					skull.MoveToWorld( GetRedSkullLocation( i ), Map );

					m_RedSkulls.Add( skull );
				}

				InvalidateProperties();
			}
		}

		public int MaxKills
		{
			get
			{
				return 250 - (Level * 12);
			}
		}

		public void SetWhiteSkullCount( int val )
		{
			for( int i = m_WhiteSkulls.Count - 1; i >= val; --i )
			{
				m_WhiteSkulls[i].Delete();
				m_WhiteSkulls.RemoveAt( i );
			}

			for( int i = m_WhiteSkulls.Count; i < val; ++i )
			{
				Item skull = new Item( 0x1854 );

				skull.Movable = false;
				skull.Light = LightType.Circle150;

				skull.MoveToWorld( GetWhiteSkullLocation( i ), Map );

				m_WhiteSkulls.Add( skull );

				Effects.PlaySound( skull.Location, skull.Map, 0x29 );
				Effects.SendLocationEffect( new Point3D( skull.X + 1, skull.Y + 1, skull.Z ), skull.Map, 0x3728, 10 );
			}
		}

		public void Start()
		{
			if( m_Active || Deleted )
				return;

			m_Active = true;
			m_HasBeenAdvanced = false;

			if( m_Timer != null )
				m_Timer.Stop();

			m_Timer = new SliceTimer( this );
			m_Timer.Start();

			if( m_RestartTimer != null )
				m_RestartTimer.Stop();

			m_RestartTimer = null;

			if( m_Altar != null )
			{
				if ( m_Champion != null )
					m_Altar.Hue = 0x26;
				else
					m_Altar.Hue = 0;
			}

			if ( m_Platform != null )
				m_Platform.Hue = 0x452;
		}

		public void Stop()
		{
			if( !m_Active || Deleted )
				return;

			m_Active = false;
			m_HasBeenAdvanced = false;

			if( m_Timer != null )
				m_Timer.Stop();

			m_Timer = null;

			if( m_RestartTimer != null )
				m_RestartTimer.Stop();

			m_RestartTimer = null;

			if( m_Altar != null )
				m_Altar.Hue = 0;

			if ( m_Platform != null )
				m_Platform.Hue = 0x497;
		}

		public void BeginRestart( TimeSpan ts )
		{
			if( m_RestartTimer != null )
				m_RestartTimer.Stop();

			m_RestartTime = DateTime.Now + ts;

			m_RestartTimer = new RestartTimer( this, ts );
			m_RestartTimer.Start();
		}

		public void EndRestart()
		{
			if( RandomizeType )
			{
				switch( Utility.Random( 5 ) )
				{
					case 0: Type = ChampionSpawnType.VerminHorde; break;
					case 1: Type = ChampionSpawnType.UnholyTerror; break;
					case 2: Type = ChampionSpawnType.ColdBlood; break;
					case 3: Type = ChampionSpawnType.Abyss; break;
					case 4: Type = ChampionSpawnType.Arachnid; break;
				}
			}
			
			m_HasBeenAdvanced = false;

			Start();
		}

		public void OnSlice()
		{
			if( !m_Active || Deleted )
				return;

			if( m_Champion != null )
			{
				if( m_Champion.Deleted )
				{
					if ( m_Platform != null )
						m_Platform.Hue = 0x497;

					if( m_Altar != null )
					{
						m_Altar.Hue = 0;
						new StarRoomGate( true, m_Altar.Location, m_Altar.Map );
					}

					m_Champion = null;
					Stop();

					BeginRestart( m_RestartDelay );
				}
			}
			else
			{
				int kills = m_Kills;

				for ( int i = 0; i < m_Creatures.Count; ++i )
				{
					Mobile m = m_Creatures[i];

					if ( m.Deleted )
					{
						m_Creatures.RemoveAt( i );
						--i;
						++m_Kills;


						Mobile killer = m.FindMostRecentDamager( false );

						if( killer is BaseCreature )
							killer = ((BaseCreature)killer).GetMaster();

						if( killer is PlayerMobile )
						{
							int mobSubLevel = GetSubLevelFor( m ) + 1;

							if( mobSubLevel >= 0 )
							{
								bool gainedPath = false;

								int pointsToGain = mobSubLevel * 40;

								if( VirtueHelper.Award( killer, VirtueName.Valor, pointsToGain, ref gainedPath ) )
								{
									if( gainedPath )
										m.SendLocalizedMessage( 1054032 ); // You have gained a path in Valor!
									else
										m.SendLocalizedMessage( 1054030 ); // You have gained in Valor!

									//No delay on Valor gains
								}

								PlayerMobile.ChampionTitleInfo info = ((PlayerMobile)killer).ChampionTitles;

								info.Award( m_Type, mobSubLevel );
							}
						}
					}
				}

				// Only really needed once.
				if ( m_Kills > kills )
					InvalidateProperties();

				double n = m_Kills / (double)MaxKills;
				int p = (int)(n * 100);

				if( p >= 90 )
					AdvanceLevel();
				else if( p > 0 )
					SetWhiteSkullCount( p / 20 );

				if( DateTime.Now >= m_ExpireTime )
					Expire();

				Respawn();
			}
		}

		public void AdvanceLevel()
		{
			m_ExpireTime = DateTime.Now + m_ExpireDelay;

			if( Level < 16 )
			{
				m_Kills = 0;
				++Level;
				InvalidateProperties();
				SetWhiteSkullCount( 0 );

				if( m_Altar != null )
				{
					Effects.PlaySound( m_Altar.Location, m_Altar.Map, 0x29 );
					Effects.SendLocationEffect( new Point3D( m_Altar.X + 1, m_Altar.Y + 1, m_Altar.Z ), m_Altar.Map, 0x3728, 10 );
				}
			}
			else
			{
				SpawnChampion();
			}
		}

		public void SpawnChampion()
		{
			if( m_Altar != null )
				m_Altar.Hue = 0x26;

			if ( m_Platform != null )
				m_Platform.Hue = 0x452;

			m_Kills = 0;
			Level = 0;
			InvalidateProperties();
			SetWhiteSkullCount( 0 );

			try
			{
				m_Champion = Activator.CreateInstance( ChampionSpawnInfo.GetInfo( m_Type ).Champion ) as Mobile;
			}
			catch { }

			if( m_Champion != null )
				m_Champion.MoveToWorld( new Point3D( X, Y, Z - 15 ), Map );
		}

		public void Respawn()
		{
			if( !m_Active || Deleted || m_Champion != null )
				return;

			while( m_Creatures.Count < (200 - (GetSubLevel() * 40)) )
			{
				Mobile m = Spawn();

				if( m == null )
					return;

				Point3D loc = GetSpawnLocation();

				// Allow creatures to turn into Paragons at Ilshenar champions.
				m.OnBeforeSpawn( loc, Map );

				m_Creatures.Add( m );
				m.MoveToWorld( loc, Map );

				if( m is BaseCreature )
				{
					BaseCreature bc = m as BaseCreature;
					bc.Tamable = false;

					if( !m_ConfinedRoaming )
					{
						bc.Home = this.Location;
						bc.RangeHome = (int)(Math.Sqrt( m_SpawnArea.Width * m_SpawnArea.Width + m_SpawnArea.Height * m_SpawnArea.Height )/2);
					}
					else
					{
						bc.Home = bc.Location;

						Point2D xWall1 = new Point2D( m_SpawnArea.X, bc.Y );
						Point2D xWall2 = new Point2D( m_SpawnArea.X + m_SpawnArea.Width, bc.Y );
						Point2D yWall1 = new Point2D( bc.X, m_SpawnArea.Y );
						Point2D yWall2 = new Point2D( bc.X, m_SpawnArea.Y + m_SpawnArea.Height );

						double minXDist = Math.Min( bc.GetDistanceToSqrt( xWall1 ), bc.GetDistanceToSqrt( xWall2 ) );
						double minYDist = Math.Min( bc.GetDistanceToSqrt( yWall1 ), bc.GetDistanceToSqrt( yWall2 ) );

						bc.RangeHome = (int)Math.Min( minXDist, minYDist );
					}
				}
			}
		}

		public Point3D GetSpawnLocation()
		{
			Map map = Map;

			if( map == null )
				return Location;

			// Try 20 times to find a spawnable location.
			for( int i = 0; i < 20; i++ )
			{
				/*
				int x = Location.X + (Utility.Random( (m_SpawnRange * 2) + 1 ) - m_SpawnRange);
				int y = Location.Y + (Utility.Random( (m_SpawnRange * 2) + 1 ) - m_SpawnRange);
				*/

				int x = Utility.Random( m_SpawnArea.X, m_SpawnArea.Width );
				int y = Utility.Random( m_SpawnArea.Y, m_SpawnArea.Height );

				int z = Map.GetAverageZ( x, y );

				if( Map.CanSpawnMobile( new Point2D( x, y ), z ) )
					return new Point3D( x, y, z );
			}

			return Location;
		}

		private const int Level1 = 5;  // First spawn level from 0-5 red skulls
		private const int Level2 = 9;  // Second spawn level from 6-9 red skulls
		private const int Level3 = 13; // Third spawn level from 10-13 red skulls

		public int GetSubLevel()
		{
			int level = this.Level;

			if( level <= Level1 )
				return 0;
			else if( level <= Level2 )
				return 1;
			else if( level <= Level3 )
				return 2;

			return 3;
		}

		public int GetSubLevelFor( Mobile m )
		{
			Type[][] types = ChampionSpawnInfo.GetInfo( m_Type ).SpawnTypes;
			Type t = m.GetType();

			for( int i = 0; i < types.GetLength( 0 ); i++ )
			{
				Type[] individualTypes = types[i];

				for( int j = 0; j < individualTypes.Length; j++ )
				{
					if( t == individualTypes[j] )
						return i;
				}
			}

			return -1;
		}

		public Mobile Spawn()
		{
			Type[][] types = ChampionSpawnInfo.GetInfo( m_Type ).SpawnTypes;

			int v = GetSubLevel();

			if( v >= 0 && v < types.Length )
				return Spawn( types[v] );

			return null;
		}

		public Mobile Spawn( params Type[] types )
		{
			try
			{
				return Activator.CreateInstance( types[Utility.Random( types.Length )] ) as Mobile;
			}
			catch
			{
				return null;
			}
		}

		public void Expire()
		{
			m_Kills = 0;

			if( m_WhiteSkulls.Count == 0 )
			{
				// They didn't even get 20%, go back a level

				if( Level > 0 )
					--Level;

				InvalidateProperties();
			}
			else
			{
				SetWhiteSkullCount( 0 );
			}

			m_ExpireTime = DateTime.Now + m_ExpireDelay;
		}

		public Point3D GetRedSkullLocation( int index )
		{
			int x, y;

			if( index < 5 )
			{
				x = index - 2;
				y = -2;
			}
			else if( index < 9 )
			{
				x = 2;
				y = index - 6;
			}
			else if( index < 13 )
			{
				x = 10 - index;
				y = 2;
			}
			else
			{
				x = -2;
				y = 14 - index;
			}

			return new Point3D( X + x, Y + y, Z - 15 );
		}

		public Point3D GetWhiteSkullLocation( int index )
		{
			int x, y;

			switch( index )
			{
				default:
				case 0: x = -1; y = -1; break;
				case 1: x =  1; y = -1; break;
				case 2: x =  1; y =  1; break;
				case 3: x = -1; y =  1; break;
			}

			return new Point3D( X + x, Y + y, Z - 15 );
		}

		public override void AddNameProperty( ObjectPropertyList list )
		{
			list.Add( "champion spawn" );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if( m_Active )
			{
				list.Add( 1060742 ); // active
				list.Add( 1060658, "Type\t{0}", m_Type ); // ~1_val~: ~2_val~
				list.Add( 1060659, "Level\t{0}", Level ); // ~1_val~: ~2_val~
				list.Add( 1060660, "Kills\t{0} of {1} ({2:F1}%)", m_Kills, MaxKills, 100.0 * ((double)m_Kills / MaxKills) ); // ~1_val~: ~2_val~
				//list.Add( 1060661, "Spawn Range\t{0}", m_SpawnRange ); // ~1_val~: ~2_val~
			}
			else
			{
				list.Add( 1060743 ); // inactive
			}
		}

		public override void OnSingleClick( Mobile from )
		{
			if( m_Active )
				LabelTo( from, "{0} (Active; Level: {1}; Kills: {2}/{3})", m_Type, Level, m_Kills, MaxKills );
			else
				LabelTo( from, "{0} (Inactive)", m_Type );
		}

		public override void OnDoubleClick( Mobile from )
		{
			from.SendGump( new PropertiesGump( from, this ) );
		}

		public override void OnLocationChange( Point3D oldLoc )
		{
			if( Deleted )
				return;

			if( m_Platform != null )
				m_Platform.Location = new Point3D( X, Y, Z - 20 );

			if( m_Altar != null )
				m_Altar.Location = new Point3D( X, Y, Z - 15 );

			if( m_Idol != null )
				m_Idol.Location = new Point3D( X, Y, Z - 15 );

			if( m_RedSkulls != null )
			{
				for( int i = 0; i < m_RedSkulls.Count; ++i )
					m_RedSkulls[i].Location = GetRedSkullLocation( i );
			}

			if( m_WhiteSkulls != null )
			{
				for( int i = 0; i < m_WhiteSkulls.Count; ++i )
					m_WhiteSkulls[i].Location = GetWhiteSkullLocation( i );
			}

			m_SpawnArea.X += Location.X - oldLoc.X;
			m_SpawnArea.Y += Location.Y - oldLoc.Y;

			UpdateRegion();
		}

		public override void OnMapChange()
		{
			if( Deleted )
				return;

			if( m_Platform != null )
				m_Platform.Map = Map;

			if( m_Altar != null )
				m_Altar.Map = Map;

			if( m_Idol != null )
				m_Idol.Map = Map;

			if( m_RedSkulls != null )
			{
				for( int i = 0; i < m_RedSkulls.Count; ++i )
					m_RedSkulls[i].Map = Map;
			}

			if( m_WhiteSkulls != null )
			{
				for( int i = 0; i < m_WhiteSkulls.Count; ++i )
					m_WhiteSkulls[i].Map = Map;
			}

			UpdateRegion();
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if( m_Platform != null )
				m_Platform.Delete();

			if( m_Altar != null )
				m_Altar.Delete();

			if( m_Idol != null )
				m_Idol.Delete();

			if( m_RedSkulls != null )
			{
				for( int i = 0; i < m_RedSkulls.Count; ++i )
					m_RedSkulls[i].Delete();

				m_RedSkulls.Clear();
			}

			if( m_WhiteSkulls != null )
			{
				for( int i = 0; i < m_WhiteSkulls.Count; ++i )
					m_WhiteSkulls[i].Delete();

				m_WhiteSkulls.Clear();
			}

			if( m_Creatures != null )
			{
				for( int i = 0; i < m_Creatures.Count; ++i )
				{
					Mobile mob = m_Creatures[i];

					if( !mob.Player )
						mob.Delete();
				}

				m_Creatures.Clear();
			}

			if( m_Champion != null && !m_Champion.Player )
				m_Champion.Delete();

			Stop();

			UpdateRegion();
		}

		public ChampionSpawn( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)4 ); // version

			writer.Write( m_ConfinedRoaming );
			writer.WriteItem<IdolOfTheChampion>( m_Idol );
			writer.Write( m_HasBeenAdvanced );
			writer.Write( m_SpawnArea );

			writer.Write( m_RandomizeType );

			//			writer.Write( m_SpawnRange );
			writer.Write( m_Kills );

			writer.Write( (bool)m_Active );
			writer.Write( (int)m_Type );
			writer.Write( m_Creatures, true );
			writer.Write( m_RedSkulls, true );
			writer.Write( m_WhiteSkulls, true );
			writer.WriteItem<ChampionPlatform>( m_Platform );
			writer.WriteItem<ChampionAltar>( m_Altar );
			writer.Write( m_ExpireDelay );
			writer.WriteDeltaTime( m_ExpireTime );
			writer.Write( m_Champion );
			writer.Write( m_RestartDelay );

			writer.Write( m_RestartTimer != null );

			if( m_RestartTimer != null )
				writer.WriteDeltaTime( m_RestartTime );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch( version )
			{
				case 4:
				{
					m_ConfinedRoaming = reader.ReadBool();
					m_Idol = reader.ReadItem<IdolOfTheChampion>();
					m_HasBeenAdvanced = reader.ReadBool();

					goto case 3;
				}
				case 3:
				{
					m_SpawnArea = reader.ReadRect2D();

					goto case 2;
				}
				case 2:
				{
					m_RandomizeType = reader.ReadBool();

					goto case 1;
				}
				case 1:
				{
					if( version < 3 )
					{
						int oldRange = reader.ReadInt();

						m_SpawnArea = new Rectangle2D( new Point2D( X - oldRange, Y - oldRange ), new Point2D( X + oldRange, Y + oldRange ) );
					}

					m_Kills = reader.ReadInt();

					goto case 0;
				}
				case 0:
				{
					if( version < 1 )
						m_SpawnArea = new Rectangle2D( new Point2D( X - 24, Y - 24 ), new Point2D( X + 24, Y + 24 ) );	//Default was 24

					bool active = reader.ReadBool();
					m_Type = (ChampionSpawnType)reader.ReadInt();
					m_Creatures = reader.ReadStrongMobileList();
					m_RedSkulls = reader.ReadStrongItemList();
					m_WhiteSkulls = reader.ReadStrongItemList();
					m_Platform = reader.ReadItem<ChampionPlatform>();
					m_Altar = reader.ReadItem<ChampionAltar>();
					m_ExpireDelay = reader.ReadTimeSpan();
					m_ExpireTime = reader.ReadDeltaTime();
					m_Champion = reader.ReadMobile();
					m_RestartDelay = reader.ReadTimeSpan();

					if( reader.ReadBool() )
					{
						m_RestartTime = reader.ReadDeltaTime();
						BeginRestart( m_RestartTime - DateTime.Now );
					}

					if( version < 4 )
					{
						m_Idol = new IdolOfTheChampion( this );
						m_Idol.MoveToWorld( new Point3D( X, Y, Z - 15 ), Map );
					}

					if( m_Platform == null || m_Altar == null || m_Idol == null )
						Delete();
					else if( active )
						Start();

					break;
				}
			}

			Timer.DelayCall( TimeSpan.Zero, new TimerCallback( UpdateRegion ) );
		}
	}

	public class ChampionSpawnRegion : BaseRegion
	{
		private ChampionSpawn m_Spawn;

		public ChampionSpawn ChampionSpawn
		{
			get { return m_Spawn; }
		}

		public ChampionSpawnRegion( ChampionSpawn spawn ) : base( null, spawn.Map, Region.Find( spawn.Location, spawn.Map ), spawn.SpawnArea )
		{
			m_Spawn = spawn;
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return false;
		}

		public override void AlterLightLevel( Mobile m, ref int global, ref int personal )
		{
			base.AlterLightLevel( m, ref global, ref personal );
			global = Math.Max( global, 1 + m_Spawn.Level );	//This is a guesstimate.  TODO: Verify & get exact values // OSI testing: at 2 red skulls, light = 0x3 ; 1 red = 0x3.; 3 = 8; 9 = 0xD 8 = 0xD 12 = 0x12 10 = 0xD
		}
	}

	public class IdolOfTheChampion : Item
	{
		private ChampionSpawn m_Spawn;

		public ChampionSpawn Spawn { get { return m_Spawn; } }

		public override string DefaultName
		{
			get { return "Idol of the Champion"; }
		}


		public IdolOfTheChampion( ChampionSpawn spawn ): base( 0x1F18 )
		{
			m_Spawn = spawn;
			Movable = false;
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if ( m_Spawn != null )
				m_Spawn.Delete();
		}

		public IdolOfTheChampion( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_Spawn );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Spawn = reader.ReadItem() as ChampionSpawn;

					if ( m_Spawn == null )
						Delete();

					break;
				}
			}
		}
	}
}