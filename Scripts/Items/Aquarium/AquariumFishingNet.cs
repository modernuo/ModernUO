using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Targeting;

namespace Server.Items
{
	public class AquariumFishingNet : Item
	{		
		public override int LabelNumber{ get{ return 1074463; } } // An aquarium fishing net
		
		private static int[] m_Hues = {	0x09B, 0x0CD, 0x0D3, 0x14D, 0x1DD, 0x1E9, 0x1F4, 0x373, 0x451, 0x47F, 0x489, 0x492, 0x4B5, 0x8AA };
		private static int[] m_WaterTiles = { 0x00A8, 0x00AB, 0x0136, 0x0137 };
		
		private Mobile m_Player;
		
		[Constructable]
		public AquariumFishingNet() : base( 0xDC8 )
		{
			Weight = 1.0;
			
			Hue = ( 0.01 > Utility.RandomDouble() ) ? Utility.RandomList( m_Hues ) : 0x240;
		}

		public AquariumFishingNet( Serial serial ) : base( serial )
		{		
		}
		
		public override void OnDoubleClick( Mobile from )
		{
			if ( !IsChildOf( from.Backpack ) )
			{
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
				return;
			}		
				
			from.SendLocalizedMessage( 1010484 ); // Where do you wish to use the net?
			from.BeginTarget( -1, true, TargetFlags.None, new TargetCallback( OnTarget ) );
		}
		
		public void OnTarget( Mobile from, object obj )
		{
			m_Player = from;
			
			if ( Deleted )
				return;

			IPoint3D p3D = obj as IPoint3D;

			if ( p3D == null )
				return;

			Map map = from.Map;

			if ( map == null || map == Map.Internal )
				return;

			int x = p3D.X, y = p3D.Y;

			if ( !from.InRange( p3D, 6 ) )
			{
				from.SendLocalizedMessage( 500976 ); // You need to be closer to the water to fish!
			}
			else if ( FullValidation( map, x, y ) )
			{
				Point3D p = new Point3D( x, y, map.GetAverageZ( x, y ) );

				Movable = false;
				MoveToWorld( p, map );

				from.Animate( 12, 5, 1, true, false, 0 );

				Timer.DelayCall( TimeSpan.FromSeconds( 1.5 ), TimeSpan.FromSeconds( 1.0 ), 20, new TimerStateCallback( DoEffect ), new object[]{ p, 0 } );

				from.SendLocalizedMessage( 1010487 ); // You plunge the net into the sea...
			}
			else
			{
				from.SendLocalizedMessage( 1010485 ); // You can only use this net in deep water!
			}
		}
		
		public static bool FullValidation( Map map, int x, int y )
		{
			bool valid = ValidateDeepWater( map, x, y );

			for ( int j = 1, offset = 5; valid && j <= 5; ++j, offset += 5 )
			{
				if ( !ValidateDeepWater( map, x + offset, y + offset ) )
					valid = false;
				else if ( !ValidateDeepWater( map, x + offset, y - offset ) )
					valid = false;
				else if ( !ValidateDeepWater( map, x - offset, y + offset ) )
					valid = false;
				else if ( !ValidateDeepWater( map, x - offset, y - offset ) )
					valid = false;
			}

			return valid;
		}

		private static bool ValidateDeepWater( Map map, int x, int y )
		{
			int tileID = map.Tiles.GetLandTile( x, y ).ID;
			bool water = false;

			for ( int i = 0; !water && i < m_WaterTiles.Length; i += 2 )
				water = ( tileID >= m_WaterTiles[i] && tileID <= m_WaterTiles[i + 1] );

			return water;
		}
		
		private void DoEffect( object state )
		{
			if ( Deleted )
				return;

			object[] states = (object[])state;

			Point3D p = (Point3D)states[0];
			int index = (int)states[1];

			states[1] = ++index;

			if ( index == 1 )
			{
				Effects.SendLocationEffect( p, Map, 0x352D, 16, 4 );
				Effects.PlaySound( p, Map, 0x364 );
			}
			else if ( index <= 10 || index == 20 )
			{
				for ( int i = 0; i < 3; ++i )
				{
					int x, y;

					switch ( Utility.Random( 8 ) )
					{
						default:
						case 0: x = -1; y = -1; break;
						case 1: x = -1; y =  0; break;
						case 2: x = -1; y = +1; break;
						case 3: x =  0; y = -1; break;
						case 4: x =  0; y = +1; break;
						case 5: x = +1; y = -1; break;
						case 6: x = +1; y =  0; break;
						case 7: x = +1; y = +1; break;
					}

					Effects.SendLocationEffect( new Point3D( p.X + x, p.Y + y, p.Z ), Map, 0x352D, 16, 4 );
				}

				Effects.PlaySound( p, Map, 0x364 );

				if ( index == 20 )
					FinishEffect( p );
				else
					this.Z -= 1;
			}
		}
		
		private void FinishEffect( Point3D p )
		{
			BaseFish fish = GiveFish( m_Player.Skills.Fishing.Base / 100 );
			
			if ( fish != null )
			{
				Item[] items = m_Player.Backpack.FindItemsByType( typeof( FishBowl ) );			
				
				foreach ( FishBowl bowl in items )
				{
					if ( !bowl.Deleted && bowl.Empty )
					{
						fish.StopTimer();
						bowl.AddItem( fish );
						bowl.InvalidateProperties();
						m_Player.SendLocalizedMessage( 1074489 ); // A live creature jumps into the fish bowl in your pack!
						Delete();
						return;
					}
				}				
				
				if ( !m_Player.PlaceInBackpack( fish ) )
				{
					m_Player.SendLocalizedMessage( 500720 ); // You don't have enough room in your backpack!
					fish.MoveToWorld( m_Player.Location, m_Player.Map );
				}
				else
					m_Player.SendLocalizedMessage( 1074490 ); // A live creature flops around in your pack before running out of air.
					
				fish.Kill();				
				Delete();
			}
			else
			{
				Movable = true;
				
				if ( !m_Player.PlaceInBackpack( this ) )
					MoveToWorld( m_Player.Location, m_Player.Map );
					
				m_Player.SendLocalizedMessage( 1074487 ); // The creatures are too quick for you!
			}
		}
		
		public BaseFish GiveFish( double skill )
		{
			if ( 0.004 * skill > Utility.RandomDouble() ) return new Shrimp();
			if ( 0.008 * skill > Utility.RandomDouble() ) return new SmallMouthSuckerFin();
			if ( 0.008 * skill > Utility.RandomDouble() ) return new SpinedScratcherFish();
			if ( 0.022 * skill > Utility.RandomDouble() ) return new SpottedBuccaneer();
			if ( 0.022 * skill > Utility.RandomDouble() ) return new YellowFinBluebelly();
			if ( 0.030 * skill > Utility.RandomDouble() ) return new BritainCrownFish();
			if ( 0.030 * skill > Utility.RandomDouble() ) return new PurpleFrog();
			if ( 0.032 * skill > Utility.RandomDouble() ) return new VesperReefTiger();
			if ( 0.038 * skill > Utility.RandomDouble() ) return new KillerFrog();
			if ( 0.040 * skill > Utility.RandomDouble() ) return new AlbinoFrog();
			if ( 0.046 * skill > Utility.RandomDouble() ) return new LongClawCrab();
			if ( 0.052 * skill > Utility.RandomDouble() ) return new SpeckledCrab();
			if ( 0.058 * skill > Utility.RandomDouble() ) return new Jellyfish();
			if ( 0.062 * skill > Utility.RandomDouble() ) return new NujelmHoneyFish();
			if ( 0.064 * skill > Utility.RandomDouble() ) return new MakotoCourtesanFish();
			if ( 0.076 * skill > Utility.RandomDouble() ) return new AlbinoCourtesanFish();
			if ( 0.082 * skill > Utility.RandomDouble() ) return new RedDartFish();
			if ( 0.088 * skill > Utility.RandomDouble() ) return new MinocBlueFish();
			if ( 0.090 * skill > Utility.RandomDouble() ) return new GoldenBroadtail();
			if ( 0.148 * skill > Utility.RandomDouble() ) return new FandancerFish();
				
			return null;
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
