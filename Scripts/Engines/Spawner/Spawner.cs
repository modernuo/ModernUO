using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Commands;
using CPA = Server.CommandPropertyAttribute;

namespace Server.Mobiles
{
	public class Spawner : Item, ISpawner
	{
		private int m_Team;
		private int m_HomeRange;
		private int m_Count;
		private int m_WalkingRange = -1;
		private TimeSpan m_MinDelay;
		private TimeSpan m_MaxDelay;

		private List<SpawnerEntry> m_Entries;
		private Dictionary<ISpawnable, SpawnerEntry> m_Spawned;

		private DateTime m_End;
		private InternalTimer m_Timer;
		private bool m_Running;
		private bool m_Group;
		private WayPoint m_WayPoint;

		public override string DefaultName{ get{ return "Spawner"; } }
		public bool IsFull{ get{ return ( m_Spawned != null && m_Spawned.Count >= m_Count ); } }
		public bool IsEmpty{ get{ return ( m_Spawned != null && m_Spawned.Count == 0 ); } }

		public Point3D HomeLocation{ get{ return Location; } }
		public bool UnlinkOnTaming{ get{ return true; } }
		public DateTime End{ get{ return m_End; } set{ m_End = value; } }

		public List<SpawnerEntry> Entries{ get{ return m_Entries; } }
		public Dictionary<ISpawnable, SpawnerEntry> Spawned{ get{ return m_Spawned; } }

		public override void OnAfterDuped( Item newItem )
		{
			if ( newItem is Spawner )
			{
				Spawner newSpawner = newItem as Spawner;
				for ( int i = 0; i < m_Entries.Count; i++ )
					newSpawner.AddEntry( m_Entries[i].SpawnedName, m_Entries[i].SpawnedProbability, m_Entries[i].SpawnedMaxCount, false );
			}
		}

		[CommandProperty( AccessLevel.Developer )]
		public int Count
		{
			get { return m_Count; }
			set
			{
				m_Count = value;

				if ( m_Timer != null )
				{
					if ( ( !IsFull && !m_Timer.Running ) || IsFull && m_Timer.Running )
						DoTimer();
				}

				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.Developer )]
		public WayPoint WayPoint{ get{ return m_WayPoint; } set{ m_WayPoint = value; } }

		[CommandProperty( AccessLevel.Developer )]
		public bool Running
		{
			get { return m_Running; }
			set
			{
				if ( value )
					Start();
				else
					Stop();

				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.Developer )]
		public int HomeRange{ get { return m_HomeRange; } set { m_HomeRange = value; InvalidateProperties(); } }

		[CommandProperty( AccessLevel.Developer )]
		public int WalkingRange{ get { return m_WalkingRange; } set { m_WalkingRange = value; InvalidateProperties(); } }

		[CommandProperty( AccessLevel.Developer )]
		public int Team{ get { return m_Team; } set { m_Team = value; InvalidateProperties(); } }

		[CommandProperty( AccessLevel.Developer )]
		public TimeSpan MinDelay{ get { return m_MinDelay; } set { m_MinDelay = value; InvalidateProperties(); } }

		[CommandProperty( AccessLevel.Developer )]
		public TimeSpan MaxDelay{ get { return m_MaxDelay; } set { m_MaxDelay = value; InvalidateProperties(); } }

		[CommandProperty( AccessLevel.Developer )]
		public TimeSpan NextSpawn
		{
			get
			{
				if ( m_Running && m_Timer != null && m_Timer.Running )
					return m_End - DateTime.UtcNow;
				else
					return TimeSpan.FromSeconds( 0 );
			}
			set
			{
				Start();
				DoTimer( value );
			}
		}

		[CommandProperty( AccessLevel.Developer )]
		public bool Group
		{
			get { return m_Group; }
			set { m_Group = value; InvalidateProperties(); }
		}

		Region ISpawner.Region{ get{ return Region.Find( Location, Map ); } }

//		[Constructible]
		public Spawner( int amount, int minDelay, int maxDelay, int team, int homeRange, string spawnedNames ) : base( 0x1f13 )
		{
			InitSpawn( amount, TimeSpan.FromMinutes( minDelay ), TimeSpan.FromMinutes( maxDelay ), team, homeRange );
			AddEntry( spawnedNames, 100, amount, false );
		}

//		[Constructible]
		public Spawner( string spawnedName ) : base( 0x1f13 )
		{
			InitSpawn( 1, TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 10 ), 0, 4 );
			AddEntry( spawnedName, 100, 1, false );
		}

		[Constructible( AccessLevel.Developer )]
		public Spawner() : base( 0x1f13 )
		{
			InitSpawn( 1, TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 10 ), 0, 4 );
		}

		public Spawner( int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, params string[] spawnedNames ) : base( 0x1f13 )
		{
			InitSpawn( amount, minDelay, maxDelay, team, homeRange );
			for ( int i = 0;i < spawnedNames.Length; i++ )
				AddEntry( spawnedNames[i], 100, amount, false );
		}

		public SpawnerEntry AddEntry( string creaturename, int probability, int amount )
		{
			return AddEntry( creaturename, probability, amount, true );
		}

		public SpawnerEntry AddEntry( string creaturename, int probability, int amount, bool dotimer )
		{
			SpawnerEntry entry = new SpawnerEntry( creaturename, probability, amount );
			m_Entries.Add( entry );
			if ( dotimer )
				DoTimer( TimeSpan.FromSeconds( 1 ) );

			return entry;
		}

		public void InitSpawn( int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange )
		{
			Visible = false;
			Movable = false;
			m_Running = true;
			m_Group = false;
			m_MinDelay = minDelay;
			m_MaxDelay = maxDelay;
			m_Count = amount;
			m_Team = team;
			m_HomeRange = homeRange;
			m_Entries = new List<SpawnerEntry>();
			m_Spawned = new Dictionary<ISpawnable, SpawnerEntry>();

			DoTimer( TimeSpan.FromSeconds( 1 ) );
		}

		public Spawner( Serial serial ) : base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel >= AccessLevel.Developer )
				from.SendGump( new SpawnerGump( this ) );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_Running )
			{
				list.Add( 1060742 ); // active

				list.Add( 1060656, m_Count.ToString() ); // amount to make: ~1_val~
				list.Add( 1061169, m_HomeRange.ToString() ); // range ~1_val~
				list.Add( 1060658, "walking range\t{0}", m_WalkingRange ); // ~1_val~: ~2_val~

				list.Add( 1060658, "group\t{0}", m_Group ); // ~1_val~: ~2_val~
				list.Add( 1060659, "team\t{0}", m_Team ); // ~1_val~: ~2_val~
				list.Add( 1060660, "speed\t{0} to {1}", m_MinDelay, m_MaxDelay ); // ~1_val~: ~2_val~

				for ( int i = 0; i < 3 && i < m_Entries.Count; ++i )
					list.Add( 1060661 + i, "{0}\t{1}", m_Entries[i].SpawnedName, CountSpawns( m_Entries[i] ) );
			}
			else
				list.Add( 1060743 ); // inactive
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			if ( m_Running )
				LabelTo( from, "[Running]" );
			else
				LabelTo( from, "[Off]" );
		}

		public void Start()
		{
			if ( !m_Running )
			{
				if ( m_Entries.Count > 0 )
				{
					m_Running = true;
					DoTimer();
				}
			}
		}

		public void Stop()
		{
			if ( m_Running )
			{
				if ( m_Timer != null )
					m_Timer.Stop();
				m_Running = false;
			}
		}

		public void Defrag()
		{
			if ( m_Entries == null )
				m_Entries = new List<SpawnerEntry>();

			for ( int i = 0; i < m_Entries.Count; ++i )
				m_Entries[i].Defrag( this );
		}

		public void OnTick()
		{
//			DoTimer( m_Spawned.Count >= m_Count );

			if ( m_Group )
			{
				Defrag();

				if ( m_Spawned.Count > 0 )
					return;

				Respawn();
			}
			else
				Spawn();
/*
			if ( m_Running && m_Timer != null )
			{
				if ( m_Spawned.Count >= m_Count && m_Timer.Running )
					DoTimer( true );
				else if ( m_Spawned.Count < m_Count && !m_Timer.Running )
					DoTimer( false );
			}
*/
			DoTimer();
		}

		public virtual void Respawn()
		{
			RemoveSpawns();

			for ( int i = 0; i < m_Count; i++ )
				Spawn();

			DoTimer(); //Turn off the timer!
		}

		public virtual void Spawn()
		{
			Defrag();

			if ( m_Entries.Count > 0 && !IsFull )
			{
				int probsum = 0;

				for ( int i = 0; i < m_Entries.Count; i++ )
					if ( !m_Entries[i].IsFull )
						probsum += m_Entries[i].SpawnedProbability;

				if ( probsum > 0 )
				{
					int rand = Utility.RandomMinMax( 1, probsum );

					for ( int i = 0; i < m_Entries.Count; i++ )
					{
						SpawnerEntry entry = m_Entries[i];
						if ( !entry.IsFull )
						{
							bool success = true;

							if ( rand <= entry.SpawnedProbability )
							{
								EntryFlags flags;
								success = Spawn( entry, out flags );
								entry.Valid = flags;
								return;
							}

							if ( success )
								rand -= entry.SpawnedProbability;
						}
					}
				}
			}
		}

		private static string[,] FormatProperties( string[] args )
		{
			string[,] props = null;

			int remains = args.Length;

			if ( remains >= 2 )
			{
				props = new string[remains / 2, 2];

				remains /= 2;

				for ( int j = 0; j < remains; ++j )
				{
					props[j, 0] = args[j * 2];
					props[j, 1] = args[(j * 2) + 1];
				}
			}
			else
				props = new string[0,0];

			return props;
		}

		private static PropertyInfo[] GetTypeProperties( Type type, string[,] props )
		{
			PropertyInfo[] realProps = null;

			if ( props != null )
			{
				realProps = new PropertyInfo[props.GetLength( 0 )];

				PropertyInfo[] allProps = type.GetProperties( BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public );

				for ( int i = 0; i < realProps.Length; ++i )
				{
					PropertyInfo thisProp = null;

					string propName = props[i, 0];

					for ( int j = 0; thisProp == null && j < allProps.Length; ++j )
					{
						if ( Insensitive.Equals( propName, allProps[j].Name ) )
							thisProp = allProps[j];
					}

					if ( thisProp == null )
						return null;
					else
					{
						CPA attr = Properties.GetCPA( thisProp );

						if ( attr == null || AccessLevel.Developer < attr.WriteLevel || !thisProp.CanWrite || attr.ReadOnly )
							return null;
						else
							realProps[i] = thisProp;
					}
				}
			}

			return realProps;
		}

		public bool Spawn( int index, out EntryFlags flags )
		{
			if ( index >= 0 && index < m_Entries.Count )
				return Spawn( m_Entries[index], out flags );
			else
			{
				flags = EntryFlags.InvalidEntry;
				return false;
			}
		}

		public bool Spawn( SpawnerEntry entry, out EntryFlags flags )
		{
			Map map = GetSpawnMap();
			flags = EntryFlags.None;

			if ( map == null || map == Map.Internal || Parent != null )
				return false;

			//Defrag taken care of in Spawn(), beforehand
			//Count check taken care of in Spawn(), beforehand

			Type type = SpawnerType.GetType( entry.SpawnedName );

			if ( type != null )
			{
				try
				{
					object o = null;
					string[] paramargs;
					string[] propargs;

					if ( String.IsNullOrEmpty( entry.Properties ) )
						propargs = new string[0];
					else
						propargs = CommandSystem.Split( entry.Properties.Trim() );

					string[,] props = FormatProperties( propargs );

					PropertyInfo[] realProps = GetTypeProperties( type, props );

					if ( realProps == null )
					{
						flags = EntryFlags.InvalidProps;
						return false;
					}

					if ( String.IsNullOrEmpty( entry.Parameters ) )
						paramargs = new string[0];
					else
						paramargs = entry.Parameters.Trim().Split( ' ' );

					ConstructorInfo[] ctors = type.GetConstructors();

					for ( int i = 0; i < ctors.Length; ++i )
					{
						ConstructorInfo ctor = ctors[i];

						if ( Add.IsConstructible( ctor, AccessLevel.Developer ) )
						{
							ParameterInfo[] paramList = ctor.GetParameters();

							if ( paramargs.Length == paramList.Length )
							{
								object[] paramValues = Add.ParseValues( paramList, paramargs );

								if ( paramValues != null )
								{
									o = ctor.Invoke( paramValues );
									for ( int j = 0; j < realProps.Length; j++ )
									{
										if ( realProps[j] != null )
										{
											object toSet = null;
											string result = Properties.ConstructFromString( realProps[j].PropertyType, o, props[j, 1], ref toSet );
											if ( result == null )
												realProps[j].SetValue( o, toSet, null );
											else
											{
												flags = EntryFlags.InvalidProps;

												if ( o is ISpawnable )
													((ISpawnable)o).Delete();

												return false;
											}
										}
									}
									break;
								}
							}
						}
					}

					if ( o is Mobile )
					{
						Mobile m = (Mobile)o;

						m_Spawned.Add( m, entry );
						entry.Spawned.Add( m );

						Point3D loc = ( m is BaseVendor ? this.Location : GetSpawnPosition( m, map ) );

						m.OnBeforeSpawn( loc, map );
						InvalidateProperties();

						m.MoveToWorld( loc, map );

						if ( m is BaseCreature )
						{
							BaseCreature c = (BaseCreature)m;

							int walkrange = GetWalkingRange();

							if( walkrange >= 0 )
								c.RangeHome = walkrange;
							else
								c.RangeHome = m_HomeRange;

							c.CurrentWayPoint = GetWayPoint();

							if ( m_Team > 0 )
								c.Team = m_Team;

							c.Home = this.Location;
							c.HomeMap = this.Map;
						}

						m.Spawner = this;
						m.OnAfterSpawn();
					}
					else if ( o is Item )
					{
						Item item = (Item)o;

						m_Spawned.Add( item, entry );
						entry.Spawned.Add( item );

						Point3D loc = GetSpawnPosition( item, map );

						item.OnBeforeSpawn( loc, map );

						item.MoveToWorld( loc, map );

						item.Spawner = this;
						item.OnAfterSpawn();
					}
					else
					{
						flags = EntryFlags.InvalidType | EntryFlags.InvalidParams;
						return false;
					}
				}
				catch ( Exception e )
				{
					Console.WriteLine( "EXCEPTION CAUGHT: {0:X}", Serial );
					Console.WriteLine( e );
					return false;
				}

				InvalidateProperties();
				return true;
			}

			flags = EntryFlags.InvalidType;
			return false;
		}

		public virtual int GetWalkingRange()
		{
			return m_WalkingRange;
		}

		public virtual WayPoint GetWayPoint()
		{
			return m_WayPoint;
		}

		public virtual Point3D GetSpawnPosition( ISpawnable spawned, Map map )
		{
			if ( map == null || map == Map.Internal )
				return Location;

			bool waterMob, waterOnlyMob;

			if ( spawned is Mobile )
			{
				Mobile mob = (Mobile)spawned;

				waterMob = mob.CanSwim;
				waterOnlyMob = ( mob.CanSwim && mob.CantWalk );
			}
			else
			{
				waterMob = false;
				waterOnlyMob = false;
			}

			// Try 10 times to find a Spawnable location.
			for ( int i = 0; i < 10; i++ )
			{
				int x = Location.X + (Utility.Random( (m_HomeRange * 2) + 1 ) - m_HomeRange);
				int y = Location.Y + (Utility.Random( (m_HomeRange * 2) + 1 ) - m_HomeRange);
				int z = Map.GetAverageZ( x, y );

				int mapZ = map.GetAverageZ( x, y );

				if ( waterMob )
				{
					if ( IsValidWater( map, x, y, this.Z ) )
						return new Point3D( x, y, this.Z );
					else if ( IsValidWater( map, x, y, mapZ ) )
						return new Point3D( x, y, mapZ );
				}

				if ( !waterOnlyMob )
				{
					if ( map.CanSpawnMobile( x, y, this.Z ) )
						return new Point3D( x, y, this.Z );
					else if ( map.CanSpawnMobile( x, y, mapZ ) )
						return new Point3D( x, y, mapZ );
				}
			}

			return this.Location;
		}

		public static bool IsValidWater( Map map, int x, int y, int z )
		{
			if ( !Region.Find( new Point3D( x, y, z ), map ).AllowSpawn() || !map.CanFit( x, y, z, 16, false, true, false ) )
				return false;

			LandTile landTile = map.Tiles.GetLandTile( x, y );

			if ( landTile.Z == z && ( TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet ) != 0 )
				return true;

			StaticTile[] staticTiles = map.Tiles.GetStaticTiles( x, y, true );

			for ( int i = 0; i < staticTiles.Length; ++i )
			{
				StaticTile staticTile = staticTiles[i];

				if ( staticTile.Z == z && ( TileData.ItemTable[staticTile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet ) != 0 )
					return true;
			}

			return false;
		}

		public virtual Map GetSpawnMap()
		{
			return Map;
		}

		public void DoTimer()
		{
			if ( !m_Running )
				return;

			int minSeconds = (int)m_MinDelay.TotalSeconds;
			int maxSeconds = (int)m_MaxDelay.TotalSeconds;

			TimeSpan delay = TimeSpan.FromSeconds( Utility.RandomMinMax( minSeconds, maxSeconds ) );
			DoTimer( delay );
		}

		public virtual void DoTimer( TimeSpan delay )
		{
			if ( !m_Running )
				return;

			m_End = DateTime.UtcNow + delay;

			if ( m_Timer != null )
				m_Timer.Stop();

			m_Timer = new InternalTimer( this, delay );
			if ( !IsFull )
				m_Timer.Start();
		}

		private class InternalTimer : Timer
		{
			private Spawner m_Spawner;

			public InternalTimer( Spawner spawner, TimeSpan delay ) : base( delay )
			{
				if ( spawner.IsFull )
					Priority = TimerPriority.FiveSeconds;
				else
					Priority = TimerPriority.OneSecond;

				m_Spawner = spawner;
			}

			protected override void OnTick()
			{
				if ( m_Spawner != null )
					if ( !m_Spawner.Deleted )
						m_Spawner.OnTick();
			}
		}

		public int CountSpawns( SpawnerEntry entry )
		{
			Defrag();

			return entry.Spawned.Count;
		}

		public void RemoveEntry( SpawnerEntry entry )
		{
			Defrag();

			for ( int i = entry.Spawned.Count-1; i >= 0; i-- )
			{
				ISpawnable e = entry.Spawned[i];
				entry.Spawned.RemoveAt( i );
				if ( e != null )
					e.Delete();
			}

			m_Entries.Remove( entry );

			if ( m_Running && !IsFull && m_Timer != null && !m_Timer.Running )
				DoTimer();

			InvalidateProperties();
		}

		public void Remove( ISpawnable spawn )
		{
			Defrag();

			if ( spawn != null )
			{
				SpawnerEntry entry;
				m_Spawned.TryGetValue( spawn, out entry );

				if ( entry != null )
					entry.Spawned.Remove( spawn );

				m_Spawned.Remove( spawn );
			}

			if ( m_Running && !IsFull && m_Timer != null && !m_Timer.Running )
				DoTimer();
		}

		public void RemoveSpawn( int index ) //Entry
		{
			if ( index >= 0 && index < m_Entries.Count )
				RemoveSpawn( m_Entries[index] );
		}

		public void RemoveSpawn( SpawnerEntry entry )
		{
			for ( int i = entry.Spawned.Count-1; i >= 0; i-- )
			{
				ISpawnable e = entry.Spawned[i];

				if ( e != null )
				{
					entry.Spawned.RemoveAt( i );
					m_Spawned.Remove( e );

					e.Delete();
				}
			}
		}

		public void RemoveSpawns()
		{
			Defrag();

			for ( int i = 0;i < m_Entries.Count; i++ )
			{
				SpawnerEntry entry = m_Entries[i];

				for ( int j = entry.Spawned.Count - 1; j >= 0; j-- )
				{
					ISpawnable e = entry.Spawned[j];

					if ( e != null )
					{
						m_Spawned.Remove( e );
						entry.Spawned.RemoveAt( j );
						e.Delete();
					}
				}
			}

			if ( m_Running && !IsFull && m_Timer != null && !m_Timer.Running )
				DoTimer();

			InvalidateProperties();
		}

		public void BringToHome()
		{
			Defrag();

			foreach( ISpawnable e in m_Spawned.Keys )
				if ( e != null )
					e.MoveToWorld( Location, Map );
		}

		public override void OnDelete()
		{
			base.OnDelete();

			Stop();
			RemoveSpawns();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 7 ); // version

			writer.Write( m_Entries.Count );

			for ( int i = 0; i < m_Entries.Count; ++i )
				m_Entries[i].Serialize( writer );

			writer.Write( m_WalkingRange );

			writer.Write( m_WayPoint );

			writer.Write( m_Group );

			writer.Write( m_MinDelay );
			writer.Write( m_MaxDelay );
			writer.Write( m_Count );
			writer.Write( m_Team );
			writer.Write( m_HomeRange );
			writer.Write( m_Running );

			if ( m_Running )
				writer.WriteDeltaTime( m_End );
		}

		private static WarnTimer m_WarnTimer;

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Spawned = new Dictionary<ISpawnable, SpawnerEntry>();

			if ( version < 7 )
				m_Entries = new List<SpawnerEntry>();

			switch ( version )
			{
				case 7:
				{
					int size = reader.ReadInt();

					m_Entries = new List<SpawnerEntry>( size );

					for ( int i = 0; i < size; ++i )
						m_Entries.Add( new SpawnerEntry( this, reader ) );

					goto case 4; //Skip the other crap
				}
				case 6:
				{
					int size = reader.ReadInt();

					bool addentries = m_Entries.Count == 0;

					for ( int i = 0; i < size; ++i )
						if ( addentries )
							m_Entries.Add( new SpawnerEntry( String.Empty, 100, reader.ReadInt() ) );
						else
							m_Entries[i].SpawnedMaxCount = reader.ReadInt();

					goto case 5;
				}
				case 5:
				{
					int size = reader.ReadInt();

					bool addentries = m_Entries.Count == 0;

					for ( int i = 0; i < size; ++i )
						if ( addentries )
							m_Entries.Add( new SpawnerEntry( String.Empty, reader.ReadInt(), 1 ) );
						else
							m_Entries[i].SpawnedProbability = reader.ReadInt();

					goto case 4;
				}
				case 4:
				{
					m_WalkingRange = reader.ReadInt();

					goto case 3;
				}
				case 3:
				case 2:
				{
					m_WayPoint = reader.ReadItem() as WayPoint;

					goto case 1;
				}

				case 1:
				{
					m_Group = reader.ReadBool();

					goto case 0;
				}

				case 0:
				{
					m_MinDelay = reader.ReadTimeSpan();
					m_MaxDelay = reader.ReadTimeSpan();
					m_Count = reader.ReadInt();
					m_Team = reader.ReadInt();
					m_HomeRange = reader.ReadInt();
					m_Running = reader.ReadBool();

					TimeSpan ts = TimeSpan.Zero;

					if ( m_Running )
						ts = reader.ReadDeltaTime() - DateTime.UtcNow;

					if ( version < 7 )
					{
						int size = reader.ReadInt();

						bool addentries = m_Entries.Count == 0;

						for ( int i = 0; i < size; ++i )
						{
							string typeName = reader.ReadString();

							if ( addentries )
								m_Entries.Add( new SpawnerEntry( typeName, 100, 1 ) );
							else
								m_Entries[i].SpawnedName = typeName;

							if ( SpawnerType.GetType( typeName ) == null )
							{
								if ( m_WarnTimer == null )
									m_WarnTimer = new WarnTimer();

								m_WarnTimer.Add( Location, Map, typeName );
							}
						}

						int count = reader.ReadInt();

						for ( int i = 0; i < count; ++i )
						{
							ISpawnable e = reader.ReadEntity() as ISpawnable;

							if ( e != null )
							{
								if ( e is BaseCreature )
									((BaseCreature)e).RemoveIfUntamed = true;

								e.Spawner = this;

								for ( int j = 0;j < m_Entries.Count; j++ )
								{
									if ( SpawnerType.GetType( m_Entries[j].SpawnedName ) == e.GetType() )
									{
										m_Entries[j].Spawned.Add( e );
										m_Spawned.Add( e, m_Entries[j] );
										break;
									}
								}
							}
						}
					}

					DoTimer( ts );

					break;
				}
			}

			if ( version < 4 )
				m_WalkingRange = m_HomeRange;
		}

		public static string ConvertTypes( string type )
		{
			type = type.ToLower();
			switch ( type )
			{
				case "wheat": return "WheatSheaf";
				case "noxxiousmage": return "NoxiousMage";
				case "noxxiousarcher": return "NoxiousArcher";
				case "noxxiouswarrior": return "NoxiousWarrior";
				case "noxxiouswarlord": return "NoxiousWarlord";
				case "obsidian": return "obsidianstatue";
				case "adeepwaterelemental": return "deepwaterelemental";
				case "noxskeleton": return "poisonskeleton";
				case "earthcaller": return "earthsummoner";
				case "bonedemon": return "bonedaemon";
			}

			return type;
		}

		private class WarnTimer : Timer
		{
			private List<WarnEntry> m_List;

			private class WarnEntry
			{
				public Point3D m_Point;
				public Map m_Map;
				public string m_Name;

				public WarnEntry( Point3D p, Map map, string name )
				{
					m_Point = p;
					m_Map = map;
					m_Name = name;
				}
			}

			public WarnTimer() : base( TimeSpan.FromSeconds( 1.0 ) )
			{
				m_List = new List<WarnEntry>();
				Start();
			}

			public void Add( Point3D p, Map map, string name )
			{
				m_List.Add( new WarnEntry( p, map, name ) );
			}

			protected override void OnTick()
			{
				try
				{
					Console.WriteLine( "Warning: {0} bad spawns detected, logged: 'badspawn.log'", m_List.Count );

					using ( StreamWriter op = new StreamWriter( "badspawn.log", true ) )
					{
						op.WriteLine( "# Bad spawns : {0}", DateTime.Now );
						op.WriteLine( "# Format: X Y Z F Name" );
						op.WriteLine();

						foreach ( WarnEntry e in m_List )
							op.WriteLine( "{0}\t{1}\t{2}\t{3}\t{4}", e.m_Point.X, e.m_Point.Y, e.m_Point.Z, e.m_Map, e.m_Name );

						op.WriteLine();
						op.WriteLine();
					}
				}
				catch
				{
				}
			}
		}
	}

	[Flags]
	public enum EntryFlags
	{
		None			= 0x000,
		InvalidType		= 0x001,
		InvalidParams	= 0x002,
		InvalidProps	= 0x004,
		InvalidEntry	= 0x008
	}

	public class SpawnerEntry
	{
		private string m_SpawnedName;
		private int m_SpawnedProbability;
		private List<ISpawnable> m_Spawned;
		private int m_SpawnedMaxCount;
		private string m_Properties;
		private string m_Parameters;
		private EntryFlags m_Valid;

		public int SpawnedProbability
		{
			get { return m_SpawnedProbability; }
			set { m_SpawnedProbability = value; }
		}

		public int SpawnedMaxCount
		{
			get { return m_SpawnedMaxCount; }
			set { m_SpawnedMaxCount = value; }
		}

		public string SpawnedName
		{
			get { return m_SpawnedName; }
			set	{ m_SpawnedName = value; }
		}

		public string Properties
		{
			get { return m_Properties; }
			set	{ m_Properties = value; }
		}

		public string Parameters
		{
			get { return m_Parameters; }
			set	{ m_Parameters = value; }
		}

		public EntryFlags Valid{ get{ return m_Valid; } set{ m_Valid = value; } }

		public List<ISpawnable> Spawned{ get { return m_Spawned; } }
		public bool IsFull{ get{ return m_Spawned.Count >= m_SpawnedMaxCount; } }

		public SpawnerEntry( string name, int probability, int maxcount )
		{
			m_SpawnedName = name;
			m_SpawnedProbability = probability;
			m_SpawnedMaxCount = maxcount;
			m_Spawned = new List<ISpawnable>();
		}

		public void Serialize( GenericWriter writer )
		{
			writer.Write( (int) 0 ); // version

			writer.Write( m_SpawnedName );
			writer.Write( m_SpawnedProbability );
			writer.Write( m_SpawnedMaxCount );

			writer.Write( m_Properties );
			writer.Write( m_Parameters );

			writer.Write( m_Spawned.Count );

			for ( int i = 0; i < m_Spawned.Count; ++i )
			{
				object o = m_Spawned[i];

				if ( o is Item )
					writer.Write( (Item)o );
				else if ( o is Mobile )
					writer.Write( (Mobile)o );
				else
					writer.Write( Serial.MinusOne );
			}
		}

		public SpawnerEntry( Spawner parent, GenericReader reader )
		{
			int version = reader.ReadInt();

			m_SpawnedName = reader.ReadString();
			m_SpawnedProbability = reader.ReadInt();
			m_SpawnedMaxCount = reader.ReadInt();

			m_Properties = reader.ReadString();
			m_Parameters = reader.ReadString();

			int count = reader.ReadInt();

			m_Spawned = new List<ISpawnable>( count );

			for ( int i = 0; i < count; ++i )
			{
				//IEntity e = World.FindEntity( reader.ReadInt() );
				ISpawnable e = reader.ReadEntity() as ISpawnable;

				if ( e != null )
				{
					e.Spawner = parent;

					if ( e is BaseCreature )
						((BaseCreature)e).RemoveIfUntamed = true;

					m_Spawned.Add( e );

					if ( !parent.Spawned.ContainsKey( e ) )
						parent.Spawned.Add( e, this );
				}
			}
		}

		public void Defrag( Spawner parent )
		{
			for ( int i = 0; i < m_Spawned.Count; ++i )
			{
				ISpawnable e = m_Spawned[i];
				bool remove = false;

				if ( e is Item )
				{
					Item item = (Item)e;

					if ( item.Deleted || item.RootParent is Mobile || item.IsLockedDown || item.IsSecure || item.Spawner == null )
						remove = true;
				}
				else if ( e is Mobile )
				{
					Mobile m = (Mobile)e;

					if ( m.Deleted )
						remove = true;
					else if ( m is BaseCreature )
					{
						BaseCreature c = (BaseCreature)m;

						if ( c.Controlled || c.IsStabled )
							remove = true;
/*
						else if ( c.Combatant == null && ( c.GetDistanceToSqrt( Location ) > (c.RangeHome * 4) ) )
						{
							//m_Spawned[i].Delete();
							m_Spawned.RemoveAt( i );
							--i;
							c.Delete();
							remove = true;
						}
*/
					}
					else if ( m.Spawner == null )
						remove = true;
				}
				else
					remove = true;

				if ( remove )
				{
					m_Spawned.RemoveAt( i-- );
					if ( parent.Spawned.ContainsKey( e ) )
						parent.Spawned.Remove( e );
				}
			}
		}
	}
}
