using System;
using System.Collections;
using Server.Commands;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
	public class LampController : Item
	{
		public static void Initialize()
		{
			CommandSystem.Register( "GenLampPuzzle", AccessLevel.Administrator, new CommandEventHandler( GenLampPuzzle_OnCommand ) );
		}

		[Usage( "GenLampPuzzle" )]
		[Description( "Generates lamp room puzzle in doom." )]
		public static void GenLampPuzzle_OnCommand( CommandEventArgs e )
		{
			e.Mobile.SendMessage( "Generating puzzle, please wait." );

			Point3D loc = new Point3D( 324, 64, -1 );
			bool exists = false;

			foreach ( Item item in Map.Malas.GetItemsInRange( loc, 0 ) )
			{
				if ( item is LampController )
				{
					exists = true;
					break;
				}
			}

			if ( !exists )
			{
				LampController controller = new LampController();
				controller.MoveToWorld( loc, Map.Malas );
				e.Mobile.SendMessage( "Puzzle generating complete. Puzzle were generated." );
			}
			else
				e.Mobile.SendMessage( "Puzzle generating complete. Puzzle aleardy exists." );
		}

		public Rectangle2D Rect = new Rectangle2D( 464, 91, 10, 10 );
		public PoisonTimer m_Timer;

		private string m_Code;
		private string m_PuzzleCode;

		[CommandProperty( AccessLevel.GameMaster )]
		public string PuzzleCode { get { return m_PuzzleCode; } set { m_PuzzleCode = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string Code
		{
			get { return m_Code; }
			set
			{
				m_Code = value;

				if ( m_Code.Length == 4 )
				{
					CheckCode();
				}
			}
		}

		private ArrayList m_Levers;
		private ArrayList m_Statues;
		private ArrayList m_Pads;
		private PuzzleBox m_Box;
		private bool m_CanActive;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool CanActive { get { return m_CanActive; } set { m_CanActive = value; } }

		private bool Check()
		{
			foreach ( Item item in World.Items.Values )
			{
				if ( item is LampController && !item.Deleted && item != this )
				{
					return true;
				}
			}

			return false;
		}

		[Constructable]
		public LampController() : base( 0x1BC3 )
		{
			if ( Check() )
			{
				World.Broadcast( 0x35, true, "Another Lamp's room controller exists in the world!" );
				Delete();
				return;
			}

			Visible = false;
			Movable = false;

			Setup();
		}

		public void Setup()
		{
			m_CanActive = true;
			m_Code = "";
			m_PuzzleCode = "";

			m_Levers = new ArrayList();
			m_Statues = new ArrayList();
			m_Pads = new ArrayList();

			PuzzleLever lever1 = new PuzzleLever( 1 );
			lever1.Controller = this;
			lever1.MoveToWorld( new Point3D( 316, 64, 5 ), Map.Malas );
			m_Levers.Add( lever1 );

			PuzzleLever lever2 = new PuzzleLever( 2 );
			lever2.Controller = this;
			lever2.MoveToWorld( new Point3D( 323, 58, 5 ), Map.Malas );
			m_Levers.Add( lever2 );

			PuzzleLever lever3 = new PuzzleLever( 3 );
			lever3.Controller = this;
			lever3.MoveToWorld( new Point3D( 332, 63, 5 ), Map.Malas );
			m_Levers.Add( lever3 );

			PuzzleLever lever4 = new PuzzleLever( 4 );
			lever4.Controller = this;
			lever4.MoveToWorld( new Point3D( 323, 71, 5 ), Map.Malas );
			m_Levers.Add( lever4 );

			PuzzleStatue statue1 = new PuzzleStatue( 0x12D8 );
			statue1.MoveToWorld( new Point3D( 319, 70, 18 ), Map.Malas );
			m_Statues.Add( statue1 );

			PuzzleStatue statue2 = new PuzzleStatue( 0x12D9 );
			statue2.MoveToWorld( new Point3D( 329, 60, 18 ), Map.Malas );
			m_Statues.Add( statue2 );

			PuzzlePad pad1 = new PuzzlePad();
			pad1.MoveToWorld( new Point3D( 324, 58, -1 ), Map.Malas );
			pad1.Visible = false;
			m_Pads.Add( pad1 );

			PuzzlePad pad2 = new PuzzlePad();
			pad2.MoveToWorld( new Point3D( 332, 64, -1 ), Map.Malas );
			pad2.Visible = false;
			m_Pads.Add( pad2 );

			PuzzlePad pad3 = new PuzzlePad();
			pad3.MoveToWorld( new Point3D( 323, 72, -1 ), Map.Malas );
			pad3.Visible = false;
			m_Pads.Add( pad3 );

			PuzzlePad pad4 = new PuzzlePad();
			pad4.MoveToWorld( new Point3D( 316, 65, -1 ), Map.Malas );
			pad4.Visible = false;
			m_Pads.Add( pad4 );

			PuzzlePad pad5 = new PuzzlePad();
			pad5.MoveToWorld( new Point3D( 324, 64, -1 ), Map.Malas );
			m_Pads.Add( pad5 );

			Teleporter teleporter1 = new Teleporter();
			teleporter1.MapDest = Map.Malas;
			teleporter1.PointDest = new Point3D( 353, 172, -1 );
			teleporter1.MoveToWorld( new Point3D( 468, 92, -1 ), Map.Malas );

			Teleporter teleporter2 = new Teleporter();
			teleporter2.MapDest = Map.Malas;
			teleporter2.PointDest = new Point3D( 353, 172, -1 );
			teleporter2.MoveToWorld( new Point3D( 469, 92, -1 ), Map.Malas );

			Teleporter teleporter3 = new Teleporter();
			teleporter3.MapDest = Map.Malas;
			teleporter3.PointDest = new Point3D( 353, 172, -1 );
			teleporter3.MoveToWorld( new Point3D( 470, 92, -1 ), Map.Malas );

			m_Box = new PuzzleBox();
			m_Box.CanSummon = true;
			m_Box.MoveToWorld( new Point3D( 469, 96, 6 ), Map.Malas );

			m_PuzzleCode = GenerateCode( m_PuzzleCode );
		}

		public void ClearRoom()
		{
			IPooledEnumerable eable = Map.Malas.GetMobilesInBounds( Rect );

			ArrayList list = new ArrayList();

			foreach ( object obj in eable )
			{
				if ( obj is Mobile )
				{
					Mobile mobile = obj as Mobile;

					list.Add( mobile );
				}
			}

			for ( int i = 0; i < list.Count; i++ )
			{
				Mobile m = list[ i ] as Mobile;

				if ( m is WandererOfTheVoid )
				{
					m.Delete();
				}
				else
				{
					Rectangle2D rect = new Rectangle2D( 342, 168, 16, 16 );

					int x = Utility.Random( rect.X, rect.Width );
					int y = Utility.Random( rect.Y, rect.Height );

					if ( x >= 345 && x <= 352 && y >= 173 && y <= 179 )
					{
						x = 353;
						y = 172;
					}

					m.MoveToWorld( new Point3D( x, y, -1 ), Map.Malas );
				}
			}

			if ( m_Timer != null )
			{
				m_Timer.Stop();
			}

			m_CanActive = true;
			m_Box.CanSummon = true;
			m_PuzzleCode = "";
			m_PuzzleCode = GenerateCode( m_PuzzleCode );
		}

		public static string[] m_Combinations = new string[] { "1234", "1243", "1324", "1342", "1423", "1432", "2134", "2143", "2314", "2341", "2413", "2431", "3124", "3142", "3214", "3241", "3412", "3421", "4123", "4132", "4213", "4231", "4312", "4321" };

		public void FreeLevers()
		{
			for ( int i = 0; i < m_Levers.Count; i++ )
			{
				PuzzleLever lever = m_Levers[ i ] as PuzzleLever;
				lever.ItemID = 0x108E;
			}

			m_Code = "";

			Timer.DelayCall( TimeSpan.FromSeconds( 30.0 ), new TimerCallback( SayQuitMessage ) );
		}

		public void SayQuitMessage()
		{
			SayStatues( 1062053, -1 ); // The sands of time have run their course.

			for ( int i = 0; i < m_Levers.Count; i++ )
			{
				PuzzleLever lever = m_Levers[ i ] as PuzzleLever;
				lever.ItemID = 0x108E;
			}
		}

		public static string GenerateCode( string puzzle )
		{
			// at OSI code scheme is right order of pressed levers
			// any press at any of 4 levers is one bit of code
			// orientation of lever doesn't play any role
			// code can't have equal numbers in it, only different, i.e: 1-4-2-3, no 1-1-1-1
			// so, we have 24 combinations for solving

			string old_code = puzzle;
			string new_code = "";

			while ( new_code == old_code )
			{
				new_code = m_Combinations[ Utility.Random( m_Combinations.Length ) ];
			}

			return new_code;
		}

		public int CompareCodes( string puzzle, string player )
		{
			int result = 0;

			if ( puzzle.Length != player.Length || puzzle.Length > 4 || player.Length > 4 )
			{
				return 0;
			}

			for ( int i = 0; i < puzzle.Length; i++ )
			{
				if ( puzzle[ i ] == player[ i ] )
				{
					result++;
				}
			}

			return result;
		}

		public void SayStatues( int message, int souls )
		{
			string args = "";

			if ( souls != -1 )
			{
				args = souls.ToString();
			}

			for ( int i = 0; i < m_Statues.Count; i++ )
			{
				PuzzleStatue statue = m_Statues[ i ] as PuzzleStatue;

				if ( statue != null )
				{
					statue.PublicOverheadMessage( Network.MessageType.Regular, 0x3B2, message, args );
				}
			}
		}

		public void CheckCode()
		{
			bool incomplete = false;

			int correct_souls = 0;

			for ( int i = 0; i < m_Pads.Count; i++ )
			{
				PuzzlePad pad = m_Pads[ i ] as PuzzlePad;

				if ( pad == null || !pad.Busy )
				{
					incomplete = true;
				}
			}

			correct_souls = CompareCodes( m_PuzzleCode, m_Code );

			if ( incomplete )
			{
				SayStatues( 1050004, -1 ); // The circle is the key, the key is incomplete and so the gate remains closed.			
			}
			else
			{
				// we don't guess code
				if ( correct_souls >= 0 && correct_souls < 4 )
				{
					ArrayList players = new ArrayList();

					for ( int i = 0; i < m_Pads.Count; i++ )
					{
						PuzzlePad pad = m_Pads[ i ] as PuzzlePad;

						if ( pad != null && pad.Stander != null && pad.Stander.Alive )
						{
							players.Add( pad.Stander );
						}
					}

					for ( int j = 0; j < players.Count; j++ )
					{
						PlayerMobile player = players[ j ] as PlayerMobile;

						if ( player != null )
						{
							Point3D location1 = player.Location;
							location1.Z = 49;

							Point3D location2 = player.Location;
							location2.Z = -1;

							Effects.SendPacket( player, player.Map, new HuedEffect( EffectType.Moving, Serial.Zero, player.Serial, 0x11B7, location1, location2, 20, 0, true, true, 0, 0 ) );
							Effects.PlaySound( new Point3D( 324, 64, -1 ), Map.Malas, 0x144 );
							Effects.PlaySound( new Point3D( player.X, player.Y, -1 ), Map.Malas, Utility.RandomList( 0x154, 0x14B ) );
							player.Send( new AsciiMessage( Serial.MinusOne, 0xFFFF, MessageType.Label, 0x66D, 3, "", "You are pinned down by the weight of the boulder!!!" ) );
						}
					}

					switch ( correct_souls )
					{
						case 0:
							SayStatues( 1050009, correct_souls );
							break; // The circle of souls has failed to turn the key.  The gate remains closed...
						case 1:
							SayStatues( 1050007, correct_souls );
							break; // ~1_NUM~ soul has turned the key correctly, but the rest have forsaken the circle...
						default:
							SayStatues( 1050008, correct_souls );
							break; // ~1_NUM~ souls have turned the key correctly, but the rest have forsaken the circle...
					}

					for ( int j = 0; j < players.Count; j++ )
					{
						PlayerMobile player = players[ j ] as PlayerMobile;

						if ( player != null )
						{
							Effects.SendPacket( player, player.Map, new HuedEffect( EffectType.FixedXYZ, Serial.Zero, Serial.Zero, 0x36BD, new Point3D( player.X, player.Y, 0 ), new Point3D( player.X, player.Y, 0 ), 20, 10, true, false, 0, 0 ) );
							Effects.SendPacket( player, player.Map, new HuedEffect( EffectType.FixedFrom, player.Serial, Serial.Zero, 0x36BD, new Point3D( player.X, player.Y, -1 ), new Point3D( player.X, player.Y, -1 ), 20, 10, true, false, 0, 0 ) );
							Effects.PlaySound( new Point3D( player.X, player.Y, -1 ), player.Map, 0x307 );

							for ( int k = 0; k < 5; k++ )
							{
								Effects.SendPacket( player, player.Map, new HuedEffect( EffectType.Moving, Serial.Zero, Serial.Zero, 0x1363 + Utility.Random( 0, 11 ), new Point3D( player.X, player.Y, 0 ), new Point3D( player.X, player.Y, 0 ), 5, 0, false, false, 0, 0 ) );
								Effects.PlaySound( new Point3D( player.X, player.Y, -1 ), Map.Malas, 0x13F );
								Effects.PlaySound( new Point3D( player.X, player.Y, -1 ), Map.Malas, 0x154 );

								player.Say( "OUCH!" );
							}

							player.Damage( 90, null );
							player.Send( new AsciiMessage( Serial.MinusOne, 0xFFFF, MessageType.Label, 0x66D, 3, "", "A speeding rock hits you in the head!" ) );
							player.SendLocalizedMessage( 502382 ); // You can move!
						}
					}
				}
				else
				{
					// we done it!
					PlayerMobile center = ( (PuzzlePad) m_Pads[ 4 ] ).Stander;

					for ( int i = 0; i < m_Pads.Count; i++ )
					{
						PuzzlePad pad = m_Pads[ i ] as PuzzlePad;

						if ( pad != null && pad.Stander != null && pad.Stander.Alive )
						{
							Effects.SendPacket( pad.Stander, Map.Malas, new HuedEffect( EffectType.FixedXYZ, Serial.Zero, Serial.Zero, 0x1153, new Point3D( 325, 64, -1 ), new Point3D( 325, 64, -1 ), 1, 60, true, false, 0, 0 ) );
							Effects.SendPacket( pad.Stander, Map.Malas, new HuedEffect( EffectType.FixedXYZ, Serial.Zero, Serial.Zero, 0x1153, new Point3D( 325, 64, -1 ), new Point3D( 325, 64, -1 ), 1, 60, true, false, 0, 0 ) );
							Effects.PlaySound( new Point3D( 325, 64, -1 ), Map.Malas, 0x244 );

							if ( center != null )
							{
								Effects.SendPacket( pad.Stander, Map.Malas, new HuedEffect( EffectType.Lightning, center.Serial, Serial.Zero, 0x0, new Point3D( 324, 64, -1 ), new Point3D( 324, 64, -1 ), 0, 0, false, false, 0, 0 ) );
								Effects.SendPacket( pad.Stander, Map.Malas, new ParticleEffect( EffectType.FixedFrom, center.Serial, Serial.Zero, 0x0, new Point3D( 324, 64, -1 ), new Point3D( 324, 64, -1 ), 0, 0, false, false, 0, 0, 0x13A7, 0, 0, center.Serial, 3, 0 ) );
							}
						}
					}

					if ( center != null )
					{
						center.MoveToWorld( new Point3D( 467, 96, -1 ), Map.Malas );

						if ( m_Timer != null )
						{
							m_Timer.Stop();
						}

						m_Timer = new PoisonTimer( this );
						m_Timer.Start();
						m_CanActive = false;
						m_Box.CanSummon = true;
					}
				}
			}

			FreeLevers();
		}

		public LampController( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.WriteItemList( m_Levers, true );
			writer.WriteItemList( m_Statues, true );
			writer.WriteItemList( m_Pads, true );

			writer.Write( m_Box );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Code = "";
			m_PuzzleCode = "";
			m_PuzzleCode = GenerateCode( m_PuzzleCode );

			m_Levers = reader.ReadItemList();
			m_Statues = reader.ReadItemList();
			m_Pads = reader.ReadItemList();

			m_Box = reader.ReadItem() as PuzzleBox;

			m_CanActive = true;
			m_Box.CanSummon = true;
		}

		public class PoisonTimer : Timer
		{
			public LampController m_Controller;
			public int count = 1;

			public PoisonTimer( LampController controller ) : base( TimeSpan.FromSeconds( 8.0 ), TimeSpan.FromSeconds( 1.0 ) )
			{
				m_Controller = controller;
			}

			public void CheckAlive()
			{
				bool AliveCreatures = false;

				IPooledEnumerable eable = Map.Malas.GetMobilesInBounds( m_Controller.Rect );

				foreach ( object obj in eable )
				{
					if ( obj is Mobile )
					{
						Mobile mobile = obj as Mobile;

						if ( mobile != null && mobile.Alive && !( mobile is WandererOfTheVoid ) )
						{
							AliveCreatures = true;
						}
					}
				}

				eable.Free();

				if ( !AliveCreatures )
				{
					m_Controller.ClearRoom();

					Stop();
				}
			}

			public void Gas( int level )
			{
				int[] x = new int[ 3 ], y = new int[ 3 ];

				for ( int i = 0; i < x.Length; i++ )
				{
					x[ i ] = Utility.Random( m_Controller.Rect.X, m_Controller.Rect.Width );
					y[ i ] = Utility.Random( m_Controller.Rect.Y, m_Controller.Rect.Height );
				}

				int hue = 0xAC;

				Poison poison = null;

				switch ( level )
				{
					case 0:
						hue = 0xA6;
						poison = Poison.Lesser;
						break;
					case 1:
						hue = 0xAA;
						poison = Poison.Regular;
						break;
					case 2:
						hue = 0xAC;
						poison = Poison.Greater;
						break;
					case 3:
						hue = 0xA8;
						poison = Poison.Deadly;
						break;
					case 4:
						hue = 0xA4;
						poison = Poison.Lethal;
						break;
					case 5:
						hue = 0xAC;
						poison = Poison.Lethal;
						break;
				}

				Effects.SendLocationParticles( EffectItem.Create( new Point3D( x[ 0 ], y[ 0 ], -1 ), Map.Malas, EffectItem.DefaultDuration ), 0x36B0, 1, Utility.Random( 160, 200 ), hue, 0, 0x1F78, 0 );
				Effects.SendLocationParticles( EffectItem.Create( new Point3D( x[ 1 ], y[ 1 ], -1 ), Map.Malas, EffectItem.DefaultDuration ), 0x36CB, 1, Utility.Random( 160, 200 ), hue, 0, 0x1F78, 0 );
				Effects.SendLocationParticles( EffectItem.Create( new Point3D( x[ 2 ], y[ 2 ], -1 ), Map.Malas, EffectItem.DefaultDuration ), 0x36BD, 1, Utility.Random( 160, 200 ), hue, 0, 0x1F78, 0 );

				IPooledEnumerable eable = Map.Malas.GetMobilesInBounds( m_Controller.Rect );

				foreach ( object obj in eable )
				{
					if ( obj is Mobile )
					{
						Mobile mobile = obj as Mobile;

						if ( mobile != null && poison != null && mobile.Poison == null && !( mobile is WandererOfTheVoid ) )
						{
							double chance = ( level + 1 ) * 0.3;

							if ( chance >= Utility.RandomDouble() )
							{
								mobile.ApplyPoison( mobile, poison );
							}
						}
					}
				}

				eable.Free();
			}

			protected override void OnTick()
			{
				CheckAlive();

				count++;

				int level = (int) ( count / 60 );

				if ( count % 60 == 0 ) // every minute we need send message to player about level's change
				{
					int number = 0;
					int hue = 0x485;

					switch ( level )
					{
						case 1:
							number = 1050001;
							break; // It is becoming more difficult for you to breathe as the poisons in the room become more concentrated.
						case 2:
							number = 1050003;
							break; // You begin to panic as the poison clouds thicken.
						case 3:
							number = 1050056;
							break; // Terror grips your spirit as you realize you may never leave this room alive.
						case 4:
							number = 1050057;
							break; // The end is near. You feel hopeless and desolate.  The poison is beginning to stiffen your muscles.
						case 5:
							number = 1062091;
							hue = 0x23F3;
							break; // The poison is becoming too much for you to bear.  You fear that you may die at any moment.
					}

					IPooledEnumerable eable = Map.Malas.GetMobilesInBounds( m_Controller.Rect );

					foreach ( object obj in eable )
					{
						if ( obj is Mobile )
						{
							Mobile mobile = obj as Mobile;

							if ( mobile != null && mobile.Player )
							{
								if ( number != 0 )
								{
									mobile.SendLocalizedMessage( number, null, hue );
								}
							}
						}
					}

					eable.Free();

					if ( level == 5 )
					{
						PainTimer timer = new PainTimer( m_Controller );

						timer.Start();
					}
				}

				if ( count % 5 == 0 ) // every 5 seconds we fill room with a gas
				{
					Gas( level );
				}
			}
		}

		public class PainTimer : Timer
		{
			public LampController m_Controller;
			public int count = 1;

			public PainTimer( LampController controller ) : base( TimeSpan.FromSeconds( 10.0 ), TimeSpan.FromSeconds( 10.0 ) )
			{
				m_Controller = controller;
			}

			protected override void OnTick()
			{
				count++;

				IPooledEnumerable eable = Map.Malas.GetMobilesInBounds( m_Controller.Rect );

				ArrayList targets = new ArrayList();

				foreach ( Mobile mobile in eable )
				{
					targets.Add( mobile );
				}

				for ( int i = 0; i < targets.Count; ++i )
				{
					Mobile mobile = targets[ i ] as Mobile;

					if ( mobile != null && !( mobile is WandererOfTheVoid ) )
					{
						if ( mobile.Player )
						{
							mobile.Say( 1062092 ); // Your body reacts violently from the pain.

							mobile.Animate( 32, 5, 1, true, false, 0 );
						}

						mobile.Damage( Utility.Random( 15, 20 ) );

						if ( count == 10 ) // at OSI at this second all mobiles is killed and room is cleared
						{
							mobile.Kill();
						}
					}
				}

				eable.Free();

				if ( count == 10 )
				{
					if ( m_Controller != null )
					{
						m_Controller.ClearRoom(); // clear room

						if ( m_Controller.m_Timer != null )
						{
							m_Controller.m_Timer.Stop(); // stop gas effects
						}

						Stop(); // stop convulsions
					}
				}
			}
		}
	}
}
