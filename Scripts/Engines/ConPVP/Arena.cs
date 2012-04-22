using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Engines.ConPVP
{
	public class ArenaController : Item
	{
		private Arena m_Arena;
		private bool m_IsPrivate;

		[CommandProperty( AccessLevel.GameMaster )]
		public Arena Arena{ get{ return m_Arena; } set{} }

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsPrivate{ get{ return m_IsPrivate; } set{ m_IsPrivate = value; } }

		public override string DefaultName
		{
			get { return "arena controller"; }
		}

		[Constructable]
		public ArenaController() : base( 0x1B7A )
		{
			Visible = false;
			Movable = false;

			m_Arena = new Arena();

			m_Instances.Add( this );
		}

		public override void OnDelete()
		{
			base.OnDelete();

			m_Instances.Remove( this );
			m_Arena.Delete();
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel >= AccessLevel.GameMaster )
				from.SendGump( new Gumps.PropertiesGump( from, m_Arena ) );
		}

		public ArenaController( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 );

			writer.Write( (bool) m_IsPrivate );

			m_Arena.Serialize( writer );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_IsPrivate = reader.ReadBool();

					goto case 0;
				}
				case 0:
				{
					m_Arena = new Arena( reader );
					break;
				}
			}

			m_Instances.Add( this );
		}

		private static List<ArenaController> m_Instances = new List<ArenaController>();

		public static List<ArenaController> Instances{ get{ return m_Instances; } set{ m_Instances = value; } }
	}

	[PropertyObject]
	public class ArenaStartPoints
	{
		private Point3D[] m_Points;

		public Point3D[] Points{ get{ return m_Points; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D EdgeWest{ get{ return m_Points[0]; } set{ m_Points[0] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D EdgeEast{ get{ return m_Points[1]; } set{ m_Points[1] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D EdgeNorth{ get{ return m_Points[2]; } set{ m_Points[2] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D EdgeSouth{ get{ return m_Points[3]; } set{ m_Points[3] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D CornerNW{ get{ return m_Points[4]; } set{ m_Points[4] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D CornerSE{ get{ return m_Points[5]; } set{ m_Points[5] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D CornerSW{ get{ return m_Points[6]; } set{ m_Points[6] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D CornerNE{ get{ return m_Points[7]; } set{ m_Points[7] = value; } }

		public override string ToString()
		{
			return "...";
		}

		public ArenaStartPoints() : this( new Point3D[8] )
		{
		}

		public ArenaStartPoints( Point3D[] points )
		{
			m_Points = points;
		}

		public ArenaStartPoints( GenericReader reader )
		{
			m_Points = new Point3D[reader.ReadEncodedInt()];

			for ( int i = 0; i < m_Points.Length; ++i )
				m_Points[i] = reader.ReadPoint3D();
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) m_Points.Length );

			for ( int i = 0; i < m_Points.Length; ++i )
				writer.Write( (Point3D) m_Points[i] );
		}
	}

	[PropertyObject]
	public class Arena : IComparable
	{
		private Map m_Facet;
		private Rectangle2D m_Bounds;
		private Rectangle2D m_Zone;
		private Point3D m_Outside;
		private Point3D m_Wall;
		private Point3D m_GateIn;
		private Point3D m_GateOut;
		private ArenaStartPoints m_Points;
		private bool m_Active;
		private string m_Name;

		private bool m_IsGuarded;

		private Item m_Teleporter;

		private List<Mobile> m_Players;

		private TournamentController m_Tournament;
		private Mobile m_Announcer;

		private LadderController m_Ladder;

		[CommandProperty( AccessLevel.GameMaster )]
		public LadderController Ladder
		{
			get{ return m_Ladder; }
			set{ m_Ladder = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsGuarded
		{
			get{ return m_IsGuarded; }
			set
			{
				m_IsGuarded = value;

				if ( m_Region != null )
					m_Region.Disabled = !m_IsGuarded;
			}
		}

		public Ladder AcquireLadder()
		{
			if ( m_Ladder != null )
				return m_Ladder.Ladder;

			return Server.Engines.ConPVP.Ladder.Instance;
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TournamentController Tournament
		{
			get{ return m_Tournament; }
			set
			{
				if ( m_Tournament != null )
					m_Tournament.Tournament.Arenas.Remove( this );

				m_Tournament = value;

				if ( m_Tournament != null )
					m_Tournament.Tournament.Arenas.Add( this );
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Announcer
		{
			get{ return m_Announcer; }
			set{ m_Announcer = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public string Name
		{
			get{ return m_Name; }
			set{ m_Name = value; if ( m_Active ) m_Arenas.Sort(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Map Facet
		{
			get{ return m_Facet; }
			set
			{
				m_Facet = value;

				if ( m_Teleporter != null )
					m_Teleporter.Map = value;

				if ( m_Region != null )
					m_Region.Unregister();

				if ( m_Zone.Start != Point2D.Zero && m_Zone.End != Point2D.Zero && m_Facet != null )
					m_Region = new SafeZone( m_Zone, m_Outside, m_Facet, m_IsGuarded );
				else
					m_Region = null;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Rectangle2D Bounds{ get{ return m_Bounds; } set{ m_Bounds = value; } }

		private SafeZone m_Region;

		public int Spectators
		{
			get
			{
				if ( m_Region == null )
					return 0;

				int specs = m_Region.GetPlayerCount() - m_Players.Count;

				if ( specs < 0 )
					specs = 0;

				return specs;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Rectangle2D Zone
		{
			get{ return m_Zone; }
			set
			{
				m_Zone = value;

				if ( m_Zone.Start != Point2D.Zero && m_Zone.End != Point2D.Zero && m_Facet != null )
				{
					if ( m_Region != null )
						m_Region.Unregister();

					m_Region = new SafeZone( m_Zone, m_Outside, m_Facet, m_IsGuarded );
				}
				else
				{
					if ( m_Region != null )
						m_Region.Unregister();

					m_Region = null;
				}
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D Outside{ get{ return m_Outside; } set{ m_Outside = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D GateIn{ get{ return m_GateIn; } set{ m_GateIn = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D GateOut{ get{ return m_GateOut; } set{ m_GateOut = value; if ( m_Teleporter != null ) m_Teleporter.Location = m_GateOut; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D Wall{ get{ return m_Wall; } set{ m_Wall = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsOccupied{ get{ return ( m_Players.Count > 0 ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public ArenaStartPoints Points{ get{ return m_Points; } set{} }

		public Item Teleporter{ get{ return m_Teleporter; } set{ m_Teleporter = value; } }

		public List<Mobile> Players{ get{ return m_Players; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Active
		{
			get{ return m_Active; }
			set
			{
				if ( m_Active == value )
					return;

				m_Active = value;

				if ( m_Active )
				{
					m_Arenas.Add( this );
					m_Arenas.Sort();
				}
				else
				{
					m_Arenas.Remove( this );
				}
			}
		}

		public void Delete()
		{
			 Active = false;

			 if ( m_Region != null )
				 m_Region.Unregister();

			 m_Region = null;
		}

		public override string ToString()
		{
			return "...";
		}

		public Point3D GetBaseStartPoint( int index )
		{
			if ( index < 0 )
				index = 0;

			return m_Points.Points[index % m_Points.Points.Length];
		}

		#region Offsets & Rotation
		private static Point2D[] m_EdgeOffsets = new Point2D[]
			{
				/*
				 *        /\
				 *       /\/\
				 *      /\/\/\
				 *      \/\/\/
				 *       \/\/\
				 *        \/\/
				 */
				new Point2D( 0, 0 ),
				new Point2D( 0, -1 ),
				new Point2D( 0, +1 ),
				new Point2D( 1, 0 ),
				new Point2D( 1, -1 ),
				new Point2D( 1, +1 ),
				new Point2D( 2, 0 ),
				new Point2D( 2, -1 ),
				new Point2D( 2, +1 ),
				new Point2D( 3, 0 )
			};

		// nw corner
		private static Point2D[] m_CornerOffsets = new Point2D[]
			{
				/*
				 *         /\
				 *        /\/\
				 *       /\/\/\
				 *      /\/\/\/\
				 *      \/\/\/\/
				 */
				new Point2D( 0, 0 ),
				new Point2D( 0, 1 ),
				new Point2D( 1, 0 ),
				new Point2D( 1, 1 ),
				new Point2D( 0, 2 ),
				new Point2D( 2, 0 ),
				new Point2D( 2, 1 ),
				new Point2D( 1, 2 ),
				new Point2D( 0, 3 ),
				new Point2D( 3, 0 )
			};

		private static int[][,] m_Rotate = new int[][,]
			{
				new int[,]{ { +1, 0 }, { 0, +1 } }, // west
				new int[,]{ { -1, 0 }, { 0, -1 } }, // east
				new int[,]{ { 0, +1 }, { +1, 0 } }, // north
				new int[,]{ { 0, -1 }, { -1, 0 } }, // south
				new int[,]{ { +1, 0 }, { 0, +1 } }, // nw
				new int[,]{ { -1, 0 }, { 0, -1 } }, // se
				new int[,]{ { 0, +1 }, { +1, 0 } }, // sw
				new int[,]{ { 0, -1 }, { -1, 0 } }, // ne
			};
		#endregion

		public void MoveInside( DuelPlayer[] players, int index )
		{
			if ( index < 0 )
				index = 0;
			else
				index %= m_Points.Points.Length;

			Point3D start = GetBaseStartPoint( index );

			int offset = 0;

			Point2D[] offsets = ( index < 4 ) ? m_EdgeOffsets : m_CornerOffsets;
			int[,] matrix = m_Rotate[index];

			for ( int i = 0; i < players.Length; ++i )
			{
				DuelPlayer pl = players[i];

				if ( pl == null )
					continue;

				Mobile mob = pl.Mobile;

				Point2D p;

				if ( offset < offsets.Length )
					p = offsets[offset++];
				else
					p = offsets[offsets.Length - 1];

				p.X = (p.X * matrix[0, 0]) + (p.Y * matrix[0, 1]);
				p.Y = (p.X * matrix[1, 0]) + (p.Y * matrix[1, 1]);

				mob.MoveToWorld( new Point3D( start.X + p.X, start.Y + p.Y, start.Z ), m_Facet );
				mob.Direction = mob.GetDirectionTo( m_Wall );

				m_Players.Add( mob );
			}
		}

		public Arena()
		{
			m_Points = new ArenaStartPoints();
			m_Players = new List<Mobile>();
		}

		public Arena( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 7:
				{
					m_IsGuarded = reader.ReadBool();

					goto case 6;
				}
				case 6:
				{
					m_Ladder = reader.ReadItem() as LadderController;

					goto case 5;
				}
				case 5:
				{
					m_Tournament = reader.ReadItem() as TournamentController;
					m_Announcer = reader.ReadMobile();

					goto case 4;
				}
				case 4:
				{
					m_Name = reader.ReadString();

					goto case 3;
				}
				case 3:
				{
					m_Zone = reader.ReadRect2D();

					goto case 2;
				}
				case 2:
				{
					m_GateIn = reader.ReadPoint3D();
					m_GateOut = reader.ReadPoint3D();
					m_Teleporter = reader.ReadItem();

					goto case 1;
				}
				case 1:
				{
					m_Players = reader.ReadStrongMobileList();

					goto case 0;
				}
				case 0:
				{
					m_Facet = reader.ReadMap();
					m_Bounds = reader.ReadRect2D();
					m_Outside = reader.ReadPoint3D();
					m_Wall = reader.ReadPoint3D();

					if ( version == 0 )
					{
						reader.ReadBool();
						m_Players = new List<Mobile>();
					}

					m_Active = reader.ReadBool();
					m_Points = new ArenaStartPoints( reader );

					if ( m_Active )
					{
						m_Arenas.Add( this );
						m_Arenas.Sort();
					}

					break;
				}
			}

			if ( m_Zone.Start != Point2D.Zero && m_Zone.End != Point2D.Zero && m_Facet != null )
				m_Region = new SafeZone( m_Zone, m_Outside, m_Facet, m_IsGuarded );

			if ( IsOccupied )
				Timer.DelayCall( TimeSpan.FromSeconds( 2.0 ), new TimerCallback( Evict ) );

			if ( m_Tournament != null )
				Timer.DelayCall( TimeSpan.Zero, new TimerCallback( AttachToTournament_Sandbox ) );
		}

		private void AttachToTournament_Sandbox()
		{
			if ( m_Tournament != null )
				m_Tournament.Tournament.Arenas.Add( this );
		}

		[CommandProperty( AccessLevel.Administrator, AccessLevel.Administrator )]
		public bool ForceEvict{ get{ return false; } set{ if ( value ) Evict(); } }

		public void Evict()
		{
			Point3D loc;
			Map facet;

			if ( m_Facet == null )
			{
				loc = new Point3D( 2715, 2165, 0 );
				facet = Map.Felucca;
			}
			else
			{
				loc = m_Outside;
				facet = m_Facet;
			}

			bool hasBounds = ( m_Bounds.Start != Point2D.Zero && m_Bounds.End != Point2D.Zero );

			for ( int i = 0; i < m_Players.Count; ++i )
			{
				Mobile mob = m_Players[i];

				if ( mob == null )
					continue;

				if ( mob.Map == Map.Internal )
				{
					if ( (m_Facet == null || mob.LogoutMap == m_Facet) && (!hasBounds || m_Bounds.Contains( mob.LogoutLocation )) )
						mob.LogoutLocation = loc;
				}
				else if ( (m_Facet == null || mob.Map == m_Facet) && (!hasBounds || m_Bounds.Contains( mob.Location )) )
				{
					mob.MoveToWorld( loc, facet );
				}

				mob.Combatant = null;
				mob.Frozen = false;
				DuelContext.Debuff( mob );
				DuelContext.CancelSpell( mob );
			}

			if ( hasBounds ) {
				List<Mobile> pets = new List<Mobile>();

				foreach ( Mobile mob in facet.GetMobilesInBounds( m_Bounds ) ) {
					BaseCreature pet = mob as BaseCreature;

					if ( pet != null && pet.Controlled && pet.ControlMaster != null ) {
						if ( m_Players.Contains( pet.ControlMaster ) ) {
							pets.Add( pet );
						}
					}
				}

				foreach ( Mobile pet in pets ) {
					pet.Combatant = null;
					pet.Frozen = false;

					pet.MoveToWorld( loc, facet );
				}
			}

			m_Players.Clear();
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 7 );

			writer.Write( (bool) m_IsGuarded );

			writer.Write( (Item) m_Ladder );

			writer.Write( (Item) m_Tournament );
			writer.Write( (Mobile) m_Announcer );

			writer.Write( (string) m_Name );

			writer.Write( (Rectangle2D) m_Zone );

			writer.Write( (Point3D) m_GateIn );
			writer.Write( (Point3D) m_GateOut );
			writer.Write( (Item) m_Teleporter );

			writer.Write( m_Players );

			writer.Write( (Map) m_Facet );
			writer.Write( (Rectangle2D) m_Bounds );
			writer.Write( (Point3D) m_Outside );
			writer.Write( (Point3D) m_Wall );
			writer.Write( (bool) m_Active );

			m_Points.Serialize( writer );
		}

		private static List<Arena> m_Arenas = new List<Arena>();

		public static List<Arena> Arenas{ get{ return m_Arenas; } }

		public static Arena FindArena( List<Mobile> players )
		{
			Preferences prefs = Preferences.Instance;

			if ( prefs == null )
				return FindArena();

			if ( m_Arenas.Count == 0 )
				return null;

			if ( players.Count > 0 )
			{
				Mobile first = players[0];

				List<ArenaController> allControllers = ArenaController.Instances;

				for ( int i = 0; i < allControllers.Count; ++i )
				{
					ArenaController controller = allControllers[i];

					if ( controller != null && !controller.Deleted && controller.Arena != null && controller.IsPrivate && controller.Map == first.Map && first.InRange( controller, 24 ) )
					{
						Multis.BaseHouse house = Multis.BaseHouse.FindHouseAt( controller );
						bool allNear = true;

						for ( int j = 0; j < players.Count; ++j )
						{
							Mobile check = players[j];
							bool isNear;

							if ( house == null )
								isNear = ( controller.Map == check.Map && check.InRange( controller, 24 ) );
							else
								isNear = ( Multis.BaseHouse.FindHouseAt( check ) == house );

							if ( !isNear )
							{
								allNear = false;
								break;
							}
						}

						if ( allNear )
							return controller.Arena;
					}
				}
			}

			List<ArenaEntry> arenas = new List<ArenaEntry>();

			for ( int i = 0; i < m_Arenas.Count; ++i )
			{
				Arena arena = m_Arenas[i];

				if ( !arena.IsOccupied )
					arenas.Add( new ArenaEntry( arena ) );
			}

			if ( arenas.Count == 0 )
				return m_Arenas[0];

			int tc = 0;

			for ( int i = 0; i < arenas.Count; ++i )
			{
				ArenaEntry ae = arenas[i];

				for ( int j = 0; j < players.Count; ++j )
				{
					PreferencesEntry pe = prefs.Find( players[j] );

					if ( pe.Disliked.Contains( ae.m_Arena.Name ) )
						++ae.m_VotesAgainst;
					else
						++ae.m_VotesFor;
				}

				tc += ae.Value;
			}

			int rn = Utility.Random( tc );

			for ( int i = 0; i < arenas.Count; ++i )
			{
				ArenaEntry ae = arenas[i];

				if ( rn < ae.Value )
					return ae.m_Arena;

				rn -= ae.Value;
			}

			return arenas[Utility.Random( arenas.Count )].m_Arena;
		}

		private class ArenaEntry
		{
			public Arena m_Arena;
			public int m_VotesFor;
			public int m_VotesAgainst;

			public int Value
			{
				get
				{
					return m_VotesFor;

					/*if ( m_VotesFor > m_VotesAgainst )
						return m_VotesFor - m_VotesAgainst;
					else if ( m_VotesFor > 0 )
						return 1;
					else
						return 0;*/
				}
			}

			public ArenaEntry( Arena arena )
			{
				m_Arena = arena;
			}
		}

		public static Arena FindArena()
		{
			if ( m_Arenas.Count == 0 )
				return null;

			int offset = Utility.Random( m_Arenas.Count );

			for ( int i = 0; i < m_Arenas.Count; ++i )
			{
				Arena arena = m_Arenas[(i + offset) % m_Arenas.Count];

				if ( !arena.IsOccupied )
					return arena;
			}

			return m_Arenas[offset];
		}

		public int CompareTo(object obj)
		{
			Arena c = (Arena)obj;

			string a = m_Name;
			string b = c.m_Name;

			if ( a == null && b == null )
				return 0;
			else if ( a == null )
				return -1;
			else if ( b == null )
				return +1;

			return a.CompareTo( b );
		}
	}
}