using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Commands;

namespace Server.Engines.Doom
{
	public class GuardianRoomRegion : BaseRegion
	{
		private static GuardianRoomRegion m_Region;

		public static void Initialize()
		{
			CommandSystem.Register( "GenGuardianRoom", AccessLevel.Administrator, new CommandEventHandler( GenRoom_OnCommand ) );
		}

		[Usage( "GenGuardianRoom" )]
		[Description( "Generates guardian room in doom." )]
		public static void GenRoom_OnCommand( CommandEventArgs e )
		{
			e.Mobile.SendMessage( "Generating room, please wait." );

			int count = 0;

			// doors
			MetalDoor north = FindItem( new GuardianRoomDoor( DoorFacing.NorthCCW ), 0, 355, 14, -1, Map.Malas, ref count ) as MetalDoor;
			MetalDoor south = FindItem( new GuardianRoomDoor( DoorFacing.SouthCW ), 0, 355, 15, -1, Map.Malas, ref count ) as MetalDoor;

			if ( north != null && south != null )
			{
				north.Link = south;
				south.Link = north;
			}

			// pentagram center
			FindItem( new Static( 0xFEA ), 0x835, 365, 15, -1, Map.Malas, ref count );

			// ghost teleporters
			Teleporter tele;
			tele = FindItem( new GhostTeleporter(), 0x835, 354, 14, -1, Map.Malas, ref count ) as Teleporter;

			if ( tele != null )
			{
				tele.PointDest = new Point3D( 349, 176, 14 );
				tele.MapDest = Map.Malas;
			}

			tele = FindItem( new GhostTeleporter(), 0x835, 354, 15, -1, Map.Malas, ref count ) as Teleporter;

			if ( tele != null )
			{
				tele.PointDest = new Point3D( 349, 176, 14 );
				tele.MapDest = Map.Malas;
			}

			// treasure chest spawner
			Spawner spawner = new Spawner( 3, 1, 5, 0, 9, "GuardianTreasureChest" );
			spawner = FindItem( spawner, 0, 365, 15, 0, Map.Malas, ref count ) as Spawner;

			if ( spawner != null )
			{
				spawner.Movable = false;
				spawner.Respawn();
			}

			if ( count > 0 )
				e.Mobile.SendMessage( "Room generating complete. {0} items were generated.", count );
			else
				e.Mobile.SendMessage( "Room generating complete. No changes neccessary." );
		}

		public static Item FindItem( Item item, int hue, int x, int y, int z, Map map, ref int count )
		{
			Point3D p = new Point3D( x, y, z );
			Type type = item.GetType();

			foreach ( Item i in map.GetItemsInRange( p, 0 ) )
			{
				if ( i.GetType() == type && i.ItemID == item.ItemID && i.Hue == hue )
				{
					item.Delete();
					return i;
				}
			}

			count++;
			item.MoveToWorld( p, map );
			
			if ( hue > 0 )
				item.Hue = hue;

			return item;
		}

		private BaseDoor m_Door;
		private List<Mobile> m_Dead;
		private Timer m_Timer;
		private int m_LightLevel;

		public List<Mobile> Dead
		{
			get { return m_Dead; }
		}

		public bool Active
		{
			get { return m_Timer != null && m_Timer.Running; }
		}

		private static Rectangle2D[] m_Bounds = new Rectangle2D[]
		{
			new Rectangle2D( new Point2D( 356, 5 ), new Point2D( 375, 25 ) )
		};

		public GuardianRoomRegion( BaseDoor door ) : base( "NorthDoomPoisonRoom", Map.Malas, 51, m_Bounds )
		{
			m_Door = door;
			m_Dead = new List<Mobile>();
			m_LightLevel = LightCycle.DungeonLevel;
			m_Guardians = new List<Mobile>();

			ExcludeFromParentSpawns = true;
		}

		public override void OnEnter( Mobile m )
		{
			if ( !Active && IsTrappable( m, true ) )
				m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( Utility.RandomMinMax( 5, 35 ) ), new TimerCallback( StartTrap ) );
		}

		public override void OnDeath( Mobile m )
		{
			base.OnDeath( m );

			if ( m.IsDeadBondedPet || ( m.Player && m.AccessLevel == AccessLevel.Player ) )
				m_Dead.Add( m );

			if ( m is DarkGuardian )
				m_Guardians.Remove( m );

			if ( m_Guardians.Count == 0 )
				Timer.DelayCall( TimeSpan.FromSeconds( 5 ), new TimerCallback( StopTrap ) );

			List<Mobile> list = GetPoisonableMobiles();
			list.Remove( m );

			if ( list.Count == 0 )
				Timer.DelayCall( TimeSpan.FromSeconds( 5 ), new TimerCallback( StopTrap ) );
		}

		public override void AlterLightLevel( Mobile m, ref int global, ref int personal )
		{
			global = m_LightLevel;
		}

		public virtual void PoisonMobiles( Poison poison, int ticks )
		{
			List<Mobile> list = GetPoisonableMobiles();
			int number = 0;
			int hue = 0x485;

			if ( ticks % 12 == 0 ) // every 60 seconds
			{
				switch ( poison.Level )
				{
					// It is becoming more difficult for you to breathe as the poisons in the room become more concentrated.
					case 1: number = 1050001; break;
					// You begin to panic as the poison clouds thicken.
					case 2: number = 1050003; break;
					// Terror grips your spirit as you realize you may never leave this room alive.
					case 3: number = 1050056; break;
					case 4:
						if ( ticks < 50 )
						{
							number = 1050057; // The end is near. You feel hopeless and desolate.  The poison is beginning to stiffen your muscles.
						}
						else
						{
							hue = 0x23F3;
							number = 1062091; // The poison is becoming too much for you to bear.  You fear that you may die at any moment.
						}

						break;
				}
			}

			foreach ( Mobile m in list )
			{
				if ( m.Player && number > 0 )
					m.SendLocalizedMessage( number, null, hue );

				if ( ( m.Poison == null || m.Poison.Level < poison.Level ) )
					m.ApplyPoison( null, poison );
			}
		}

		public virtual void HurtMobiles( int level )
		{
			foreach ( Mobile m in GetPoisonableMobiles() )
			{
				if ( m.Player )
				{
					m.Say( 1062092 ); // Your body reacts violently from the pain.
					m.Animate( 32, 5, 1, true, false, 0 );
				}

				m.Damage( Utility.Random( 15, 20 ) );

				if ( level >= 10 )
					m.Kill();
			}
		}

		public virtual List<Mobile> GetPoisonableMobiles()
		{
			List<Mobile> list = new List<Mobile>();

			foreach ( Mobile m in GetMobiles() )
			{
				if ( IsTrappable( m, false ) )
					list.Add( m );
			}

			return list;
		}

		public virtual bool IsTrappable( Mobile m, bool trapStart )
		{
			if ( m.Alive )
			{
				if ( m.Player && m.AccessLevel == AccessLevel.Player )
					return true;

				BaseCreature bc = m as BaseCreature;

				if ( bc != null )
				{
					Mobile master = null;

					if ( bc.Controlled && bc.ControlMaster != null )
						master = bc.ControlMaster;
					else if ( !trapStart && bc.Summoned && bc.SummonMaster != null )
						master = bc.SummonMaster;

					if ( master != null )
						return master.Player && master.AccessLevel == AccessLevel.Player;
				}
			}

			return false;
		}

		public virtual bool CanStart( List<Mobile> list )
		{
			foreach ( Mobile m in list )
				if ( IsTrappable( m, true ) )
					return true;

			return false;
		}

		public virtual void RestartTrap()
		{
			if ( !Active )
				StartTrap();
		}

		public virtual void StartTrap()
		{
			List<Mobile> list = GetPoisonableMobiles();

			if ( !CanStart( list ) )
				return;

			m_Timer = new PoisonRoomTimer( this );
			m_Timer.Start();

			if ( m_Door != null )
			{
				m_Door.Locked = true;
				m_Door.Open = false;

				if ( m_Door.Link != null )
				{
					m_Door.Link.Locked = true;
					m_Door.Link.Open = false;

					Effects.PlaySound( m_Door.Link.Location, Map, 0x1FF );
				}

				Effects.PlaySound( m_Door.Location, Map, 0x1FF );
			}

			foreach ( Mobile m in list )
				m.SendLocalizedMessage( 1050000, null, 0x41 ); // The locks on the door click loudly and you begin to hear a faint hissing near the walls.

			for ( int i = m_Dead.Count - 1; i >= 0; i-- )
				if ( m_Dead[ i ].Alive )
					m_Dead.RemoveAt( i );

			SpawnGuardians( list.Count * 2 );
		}

		public virtual void StopTrap()
		{
			Timer.DelayCall( TimeSpan.FromSeconds( Utility.RandomMinMax( 30, 60 ) ), new TimerCallback( RestartTrap ) );

			if ( m_Door != null )
			{
				m_Door.Locked = false;

				if ( m_Door.Link != null )
				{
					m_Door.Link.Locked = false;

					Effects.PlaySound( m_Door.Link.Location, Map, 0x1FF );
				}

				Effects.PlaySound( m_Door.Location, Map, 0x1FF );
			}

			if ( m_Timer != null && m_Timer.Running )
				m_Timer.Stop();

			foreach ( Mobile m in GetPlayers() )
				m.SendLocalizedMessage( 1050055, null, 0x41 ); // You hear the doors unlocking and the hissing stops.

			ClearGuardians();
		}

		#region Spawns
		private List<Mobile> m_Guardians;

		public void SpawnGuardians( int amount )
		{
			for ( int i = 0; i < amount; ++i )
			{
				DarkGuardian guardian = new DarkGuardian();

				switch ( Utility.Random( 4 ) )
				{
					case 0: guardian.MoveToWorld( new Point3D( 364, 15, -1 ), Map.Malas ); break;
					case 1: guardian.MoveToWorld( new Point3D( 366, 15, -1 ), Map.Malas ); break;
					case 2: guardian.MoveToWorld( new Point3D( 365, 14, -1 ), Map.Malas ); break;
					case 3: guardian.MoveToWorld( new Point3D( 365, 16, -1 ), Map.Malas ); break;
				}

				m_Guardians.Add( guardian );
			}
		}

		public void ClearGuardians()
		{
			foreach ( Mobile m in m_Guardians )
			{
				if ( m.Alive && !m.Deleted )
				{
					Effects.SendLocationParticles( EffectItem.Create( m.Location, m.Map, EffectItem.DefaultDuration ), 0x3728, 8, 20, 5042 );
					Effects.PlaySound( m, m.Map, 0x201 );

					m.Delete();
				}
			}

			m_Guardians.Clear();
		}
		#endregion

		#region Effects
		private static Point3D[] m_GasLocations = new Point3D[]
		{
			// west
			new Point3D( 356, 7, -1 ), new Point3D( 356, 13, -1 ),
			new Point3D( 356, 16, -1 ), new Point3D( 356, 22, -1 ),

			// north
			new Point3D( 358, 6, -1 ), new Point3D( 363, 6, -1 ),
			new Point3D( 368, 6, -1 ), new Point3D( 373, 6, -1 )
		};

		public virtual void DoGasEffect()
		{
			for ( int i = Utility.RandomMinMax( 2, 4 ); i > 0; i-- )
			{
				int pos = Utility.Random( m_GasLocations.Length );
				int itemID;

				if ( pos <= 3 )
					itemID = 0x1145;
				else
					itemID = 0x113A;

				Effects.SendLocationParticles( EffectItem.Create( m_GasLocations[ pos ], Map, EffectItem.DefaultDuration ), itemID, 1, 100, 0, 4, 0x139D, 0 );
			}
		}

		private static int[] m_PoisonEffects = new int[]
		{
			0x36B0, 0x36BD, 0x36CB
		};

		public virtual void DoPoisonEffect( int level )
		{
			int hue = 0;

			switch ( level )
			{
				default:
				case 0: hue = 0xA6; break;
				case 1: hue = 0xAA; break;
				case 2: hue = 0xAC; break;
				case 3: hue = 0xA8; break;
				case 4: hue = 0xA4; break;
			}

			for ( int i = Utility.RandomMinMax( 5, 7 ); i > 0; i-- )
			{
				Point3D p = RandomSpawnLocation( 0, true, true, Point3D.Zero, 0 );

				if ( p != Point3D.Zero )
				{
					IEntity e = EffectItem.Create( p, Map, EffectItem.DefaultDuration );
					int itemID = Utility.RandomList( m_PoisonEffects );
					int duration = Utility.RandomMinMax( 150, 200 );

					Effects.SendLocationParticles( e, itemID, 1, duration, hue, 0, 0x139D, 0 );
				}
			}
		}
		#endregion

		public class PoisonRoomTimer : Timer
		{
			private GuardianRoomRegion m_Room;
			private int m_Count;

			public PoisonRoomTimer( GuardianRoomRegion room ) : base( TimeSpan.FromSeconds( 1 ), TimeSpan.FromSeconds( 1 ) )
			{
				m_Room = room;
				m_Count = 0;
			}

			public int CurrentPoisonLevel()
			{
				int poisonLevel = 1 + m_Count / 20;

				if ( poisonLevel > 4 )
					poisonLevel = 4;

				return poisonLevel;
			}

			protected override void OnTick()
			{
				m_Count++;

				if ( m_Count % 5 == 0 && m_Room.Active )
				{
					int level = CurrentPoisonLevel();

					m_Room.PoisonMobiles( Poison.GetPoison( level ), m_Count / 5 );
				}

				if ( m_Count % 8 == 0 && m_Room.Active )
				{
					m_Room.DoPoisonEffect( CurrentPoisonLevel() );
					m_Room.DoGasEffect();
				}

				if ( m_Count >= 720 && m_Count % 10 == 0 && m_Room.Active )
					m_Room.HurtMobiles( ( m_Count - 720 ) / 10 );
			}
		}

		public class GhostTeleporter : Teleporter
		{
			private static BaseRegion m_GhostRegion;

			private static Rectangle2D[] m_GhostRegionBounds = new Rectangle2D[]
			{
				new Rectangle2D( new Point2D( 345, 180 ), new Point2D( 352, 184 ) ),
				new Rectangle2D( new Point2D( 342, 172 ), new Point2D( 344, 181 ) ),
				new Rectangle2D( new Point2D( 345, 169 ), new Point2D( 352, 172 ) )
			};

			[Constructable]
			public GhostTeleporter() : base()
			{
				Name = "GhostTeleporter";

				if ( m_GhostRegion == null )
				{
					m_GhostRegion = new BaseRegion( "DoomGhostTeleportRegion", Map.Malas, 55, m_GhostRegionBounds );
					m_GhostRegion.Register();
				}
			}

			public GhostTeleporter( Serial serial ) : base( serial )
			{
			}

			public override bool OnMoveOver( Mobile m )
			{
				if ( !DiedInside( m ) )
					return true;

				return base.OnMoveOver( m );
			}

			public bool DiedInside( Mobile m )
			{
				bool valid = false;

				if ( m_Region != null )
					valid = m_Region.Dead.Remove( m );

				return valid && !m.Alive;
			}

			public override void DoTeleport( Mobile m )
			{
				if ( m.Corpse != null && !m.Corpse.Deleted )
				{
					Point3D location = Point3D.Zero;

					if ( m_GhostRegion != null )
						location = m_GhostRegion.RandomSpawnLocation( 0, true, false, Point3D.Zero, 0 );

					if ( location == Point3D.Zero )
						location = new Point3D( 349, 176, 14 );

					m.Corpse.MoveToWorld( location, Map.Malas );
					m.MoveToWorld( location, Map.Malas );
				}
			}

			public override void Serialize( GenericWriter writer )
			{
				base.Serialize( writer );

				writer.WriteEncodedInt( 0 ); // version
			}

			public override void Deserialize( GenericReader reader )
			{
				base.Deserialize( reader );

				int version = reader.ReadEncodedInt();

				if ( m_GhostRegion == null )
				{
					m_GhostRegion = new BaseRegion( "DoomGhostTeleportRegion", Map.Malas, 55, m_GhostRegionBounds );
					m_GhostRegion.Register();
				}
			}
		}

		public class GuardianRoomDoor : MetalDoor
		{
			public GuardianRoomDoor( DoorFacing facing ) : base( facing )
			{
				if ( m_Region == null )
				{
					m_Region = new GuardianRoomRegion( this );
					m_Region.Register();
				}
			}

			public GuardianRoomDoor( Serial serial ) : base( serial )
			{
			}

			public override void Serialize( GenericWriter writer )
			{
				base.Serialize( writer );

				writer.WriteEncodedInt( 0 ); // version
			}

			public override void Deserialize( GenericReader reader )
			{
				base.Deserialize( reader );

				int version = reader.ReadEncodedInt();

				Locked = false;

				if ( m_Region == null )
				{
					m_Region = new GuardianRoomRegion( this );
					m_Region.Register();
				}
			}
		}
	}
}
