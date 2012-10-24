using System;
using Server.Items;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Engines.Events
{
	public class HalloweenHauntings : Item
	{
		public static Dictionary<PlayerMobile, ZombieSkeleton> ReAnimated { get { return m_ReAnimated; } set { m_ReAnimated = value; } }

		private static bool m_Running;
		private static bool m_AutoStart;

		private static DateTime m_Start;
		private static DateTime m_Finish;

		private static Timer m_Timer;
		private static Timer m_ClearTimer;

		private static int m_TotalZombieLimit;
		private static int m_DeathQueueLimit;
		private static int m_QueueDelaySeconds;
		private static int m_QueueClearIntervalSeconds;

		private static Dictionary<PlayerMobile, ZombieSkeleton> m_ReAnimated;

		private static List<PlayerMobile> m_DeathQueue;

		private static Rectangle2D[] m_Cemetaries = new Rectangle2D[]
		{
			new Rectangle2D(1272,3712,30,20), // Jhelom
			new Rectangle2D(1337,1444,48,52), // Britain
			new Rectangle2D(2424,1098,20,28), // Trinsic
			new Rectangle2D(2728,840,54,54), // Vesper
			new Rectangle2D(4528,1314,20,28), // Moonglow
			new Rectangle2D(712,1104,30,22), // Yew
			new Rectangle2D(5824,1464,22,6), // Fire Dungeon
			new Rectangle2D(5224,3655,14,5), // T2A
		};

		public static void Initialize()
		{
			DateTime today = DateTime.Now;
			TimeSpan tick = TimeSpan.FromSeconds( m_QueueDelaySeconds );
			TimeSpan clear = TimeSpan.FromSeconds( m_QueueClearIntervalSeconds );

			m_Start = new DateTime( 2011, 10, 25 );
			m_Finish = new DateTime( 2012, 11, 15 );

			m_ReAnimated = new Dictionary<PlayerMobile, ZombieSkeleton>();
			m_DeathQueue = new List<PlayerMobile>();

			if( today >= m_Start && today <= m_Finish )
			{
				m_Timer = Timer.DelayCall( tick, tick, new TimerCallback( Timer_Callback ) );

				m_ClearTimer = Timer.DelayCall( clear, clear, new TimerCallback( Clear_Callback ) );

				EventSink.PlayerDeath += new PlayerDeathEventHandler( EventSink_PlayerDeath );
			}

			m_TotalZombieLimit = 200;
			m_DeathQueueLimit = 200;
			m_QueueDelaySeconds = 2;
			m_QueueClearIntervalSeconds = 1800;
		}

		public static void Command_Stop( Mobile mobile )
		{
			m_Timer.Stop();
		}

		public static void Command_Start( Mobile mobile )
		{
			m_Timer.Start();
		}

		public static void EventSink_PlayerDeath( PlayerDeathEventArgs e )
		{
			if( e.Mobile != null && !e.Mobile.Deleted ) /* not sure .. better safe than sorry? */
			{
				PlayerMobile player = e.Mobile as PlayerMobile;

				if( m_Timer.Running && !m_DeathQueue.Contains( player ) && m_DeathQueue.Count < m_DeathQueueLimit )
				{
					m_DeathQueue.Add( player );
				}
			}
		}

		private static void Clear_Callback()
		{
			m_ReAnimated.Clear();

			m_DeathQueue.Clear();
		}

		private static void Timer_Callback()
		{
			PlayerMobile player = null;

			if( DateTime.Now < m_Finish )
			{
				for( int index = 0; m_DeathQueue.Count > 0 && index < m_DeathQueue.Count; index++ )
				{
					if( !m_ReAnimated.ContainsKey( m_DeathQueue[ index ] ) )
					{
						player = m_DeathQueue[ index ];

						break;
					}
				}

				if( player != null && !player.Deleted && m_ReAnimated.Count < m_TotalZombieLimit )
				{
					Map map = Utility.RandomBool() ? Map.Trammel : Map.Felucca;

					m_ReAnimated.Add( player, new ZombieSkeleton( player ) );
					m_ReAnimated[ player ].MoveToWorld( GetRandomPointInRect( m_Cemetaries[ Utility.Random( m_Cemetaries.Length ) ], map ), map );

					m_DeathQueue.Remove( player );
				}
			}
			else
			{
				m_Timer.Stop();
			}
		}

		private static Point3D GetRandomPointInRect( Rectangle2D rect, Map map )
		{
			int x = Utility.Random( rect.X, rect.Width );
			int y = Utility.Random( rect.Y, rect.Height );

			return new Point3D( x, y, map.GetAverageZ( x, y ) );
		}
	}

	[CorpseName( "a rotting corpse" )]
	public class ZombieSkeleton : BaseCreature
	{
		private static readonly string m_Name = "Zombie Skeleton";

		private PlayerMobile m_DeadPlayer;

		public ZombieSkeleton()
			: this( null )
		{
		}

		public class PlayerBones : Container
		{
			public PlayerBones( String name )
				: base( Utility.RandomMinMax( 0x0ECA, 0x0ED2 ) )
			{
				Name = String.Format( "{0}\'s bones", name );

				switch( Utility.Random( 10 ) )
				{
					case 0: Hue = 0xa09; break;
					case 1: Hue = 0xa93; break;
					case 2: Hue = 0xa47; break;
					default: break;
				}
			}

			public PlayerBones( Serial serial )
				: base( serial )
			{
			}

			public override void Serialize( GenericWriter writer )
			{
				base.Serialize( writer );
				writer.Write( ( int )0 );
			}

			public override void Deserialize( GenericReader reader )
			{
				base.Deserialize( reader );
				int version = reader.ReadInt();
			}
		}

		public ZombieSkeleton( PlayerMobile player )
			: base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			m_DeadPlayer = player;

			Name = ( player != null ) ? String.Format( "{0}\'s {1}", player.Name, m_Name ) : m_Name;

			Body = 0x93;
			BaseSoundID = 0x1c3;

			SetStr( 500 );
			SetDex( 500 );
			SetInt( 500 );

			SetHits( 2500 );
			SetMana( 500 );
			SetStam( 500 );

			SetDamage( 8, 18 );

			SetDamageType( ResistanceType.Physical, 40 );
			SetDamageType( ResistanceType.Cold, 60 );

			SetResistance( ResistanceType.Fire, 50 );
			SetResistance( ResistanceType.Energy, 50 );
			SetResistance( ResistanceType.Physical, 50 );
			SetResistance( ResistanceType.Cold, 50 );
			SetResistance( ResistanceType.Poison, 50 );

			SetSkill( SkillName.MagicResist, 65.1, 80.0 );
			SetSkill( SkillName.Tactics, 95.1, 100 );
			SetSkill( SkillName.Wrestling, 85.1, 95 );

			Fame = 1000;
			Karma = -1000;

			VirtualArmor = 18;
		}

		public override void GenerateLoot()
		{
			switch( Utility.Random( 10 ) )
			{
				case 0: PackItem( new LeftArm() ); break;
				case 1: PackItem( new RightArm() ); break;
				case 2: PackItem( new Torso() ); break;
				case 3: PackItem( new Bone() ); break;
				case 4: PackItem( new RibCage() ); break;
				case 5: PackItem( new PlayerBones( m_DeadPlayer.Name ) ); break;
				default: break;
			}

			AddLoot( LootPack.Meager );
		}

		public override bool BleedImmune { get { return true; } }

		public override Poison PoisonImmune { get { return Poison.Regular; } }

		public ZombieSkeleton( Serial serial )
			: base( serial )
		{
		}

		public override void OnDelete()
		{
			if( HalloweenHauntings.ReAnimated != null )
			{
				if( m_DeadPlayer != null && !m_DeadPlayer.Deleted )
				{
					if( HalloweenHauntings.ReAnimated.Count > 0 && HalloweenHauntings.ReAnimated.ContainsKey( m_DeadPlayer ) )
					{
						HalloweenHauntings.ReAnimated.Remove( m_DeadPlayer );
					}
				}
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( ( int )0 );

			writer.WriteMobile( m_DeadPlayer );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			m_DeadPlayer = ( PlayerMobile )reader.ReadMobile();
		}
	}
}