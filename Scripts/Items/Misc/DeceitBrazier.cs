/***************************************************************************
*		DeceitBrazier.cs
*	Author 			: phoenix_smasher7 (ps7)
*	Version			: 3.0
*	
****************************************************************************/

/***************************************************************************
* 	Description:
*
*	Just a little spawning system OSI made and which RunUO lacked.
*	DClicking brazier will spawn a random creature listed in m_Creatures.
*
****************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/
 
 /***************************************************************************
 * Clilocs used:
 * 
 * 500760 - The brazier fizzes and pops, but nothing seems to happen.
 * 500761 - Heed this warning well, and use this brazier at your own peril.
 *
 ***************************************************************************/
 
 /***************************************************************************
 * Changelog:
 * 
 * v1.5
 * Label was incorrect.Corrected.
 * Brazier will now only warn you if brazier can spawn another creature.
 * Added many other creatures to spawn list.
 * v1.6
 * Added missing elementals.
 * Removed nonagressive critters.
 * v2.0
 * Added some other missing creatures.
 * Added spawn range so that it will not always spawn on top of the brazier.
 * Added the flamestrike effect when creature spawned.
 * v2.1
 * Removed Jukas from the list since they no longer spawn in Britannia.
 * v2.2
 * Code cleanup
 * v2.3
 * Changed cooldown message to display as overhead message.
 * v2.4
 * Added ability to control usage of the brazier.By returning the value to true 
 * in game you will be able to use the brazier again without having to wait for 15 minutes.
 * v3.0
 * Medium code change.
 * Made the spawner more customizable so it can fit others needs too.
 * Just set the CanTalk true and Delay to anything you want ingame and use it and you are ready to go.
 * Also fixed the serialization error.
 * It is highly recommended that you remove any trace left by older scripts.
 * v3.1
 * Removed the delay between warnings.
 *
 ***************************************************************************/
 
 /***************************************************************************
* Notes:
* 
* No Matter what I've tried I have failed to create a timer that will queue the effects.In OSI it's like
* FireColumn01 Spawns
* 1 Second passes
* FireColumn02 Spawns along with our creature. // At least this is the Information I got from Erica.
*	If someone else could confirm it would remove further doubts.
* Where I couldn't succeed was the coordinates where our second effect and creature spawn.
* Creatures kept spawning at internal with coordinates of x0y0z0.
* If anyone is willing to help out with that one it's more than appreciated.
*
****************************************************************************/
 
using System;
using Server;
using Server.Misc; 
using Server.Network; 
using System.Collections; 
using Server.Mobiles;

namespace Server.Items
{
	public class DeceitBrazier : Item
	{
		private Timer m_Timer;
		private bool m_CanTalk;
		private int m_SpawnRange;
		private DateTime m_LastUse = DateTime.MinValue;
		private TimeSpan m_Delay;
		private DateTime m_NextUse;
		
		[CommandProperty( AccessLevel.Counselor, AccessLevel.GameMaster )]
		public DateTime LastUse
		{
			get{ return m_LastUse; }
			set{ m_LastUse = value; }
		}
		
		[CommandProperty(AccessLevel.GameMaster)]
		public DateTime NextUse
		{
			get { return m_NextUse; }
		}
		
		[CommandProperty(AccessLevel.GameMaster)]
		public TimeSpan Delay
		{
			get { return m_Delay; }
			set
			{
				m_NextUse = m_NextUse - m_Delay;
				m_Delay = value;
				m_NextUse = m_NextUse + m_Delay;
			}
		}
		
		[CommandProperty(AccessLevel.GameMaster)]
		public bool CanTalk
			{
				get{ return m_CanTalk; }
				set
				{
					m_CanTalk = value;
				}
			}

		private static Type[] m_Creatures = new Type[]
			{
				#region Animals
				typeof( FireSteed ), //Set the tents up people!
				#endregion
				
				#region Undead
				typeof( Skeleton ), 		typeof( SkeletalKnight ), 		typeof( SkeletalMage ), 		typeof( Mummy ),
				typeof( BoneKnight ), 		typeof( Lich ), 				typeof( LichLord ), 			typeof( BoneMagi ), 		
				typeof( Wraith ), 			typeof( Shade ), 				typeof( Spectre ), 				typeof( Zombie ),
				typeof( RottingCorpse ),	typeof( Ghoul ),
				#endregion
				
				#region Demons
				typeof( Balron ), 			typeof( Daemon ),				typeof( Imp ),					typeof( GreaterMongbat ),
				typeof( Mongbat ), 			typeof( IceFiend ), 			typeof( Gargoyle ), 			typeof( StoneGargoyle ), 
				typeof( FireGargoyle ), 	typeof( HordeMinion ), 				
				#endregion
				
				#region Gazers
				typeof( Gazer ), 			typeof( ElderGazer ), 			typeof( GazerLarva ),  
				#endregion
				
				#region Uncategorized
				typeof( Harpy ),			typeof( StoneHarpy ), 			typeof( HeadlessOne ),			typeof( HellHound ),		
				typeof( HellCat ),			typeof( Phoenix ),				typeof( LavaLizard ),			typeof( SandVortex ),		
				typeof( ShadowWisp ),		typeof( SwampTentacle ),		typeof( PredatorHellCat ),		typeof( Wisp ),
				#endregion
				
				#region Arachnid
				typeof( GiantSpider ), 		typeof( DreadSpider ), 			typeof( FrostSpider ), 			typeof( Scorpion ),
				#endregion
				
				#region Repond
				typeof( ArcticOgreLord ), 	typeof( Cyclops ), 				typeof( Ettin ), 				typeof( EvilMage ), 		
				typeof( FrostTroll ), 		typeof( Ogre ), 				typeof( OgreLord ), 			typeof( Orc ), 				 
				typeof( OrcishLord ), 		typeof( OrcishMage ), 			typeof( OrcBrute ),				typeof( Ratman ), 			  
				typeof( RatmanMage ),		typeof( OrcCaptain ),			typeof( Troll ),				typeof( Titan ),
				typeof( EvilMageLord ), 	typeof( OrcBomber ),			typeof( RatmanArcher ),
				#endregion
				
				#region Reptilian
				typeof( Dragon ), 			typeof( Drake ), 				typeof( Snake ),				typeof( GreaterDragon ),
				typeof( IceSerpent ), 		typeof( GiantSerpent ), 		typeof( IceSnake ), 			typeof( LavaSerpent ), 		  
				typeof( Lizardman ), 		typeof( Wyvern ),				typeof( WhiteWyrm ), 
				typeof( ShadowWyrm ), 		typeof( SilverSerpent ), 		typeof( LavaSnake ),	
				#endregion
				
				#region Elementals
				typeof( EarthElemental ), 	typeof( PoisonElemental ),		typeof( FireElemental ),		typeof( SnowElemental ),
				typeof( IceElemental ),		typeof( ToxicElemental ),		typeof( WaterElemental ),		typeof( Efreet ),
				typeof( AirElemental ),		typeof( Golem ),
				#endregion
				
				#region Random Critters
				typeof( Sewerrat ),			typeof( GiantRat ), 			typeof( DireWolf ),				typeof( TimberWolf ), 		  		
				typeof( Cougar ), 			typeof( Alligator )		 
				#endregion
			};

		public static Type[] Creatures { get { return m_Creatures; } }
		
		public override int LabelNumber{ get{ return 1023633; } } // Brazier
		
		[Constructable]
		public DeceitBrazier() : base( 0xE31 )
		{
			Movable = false;
			Light = LightType.Circle225;
		}

		public DeceitBrazier( Serial serial ) : base( serial )
		{
		}
	    
		public override void OnDoubleClick( Mobile from )
		{
			if ( from.InRange( GetWorldLocation(), 2 ) )
			{					
				Mobile m = null;
					try
					{
						if(m_NextUse <= DateTime.Now)
						{
							Map map = Map;
							m = Activator.CreateInstance( m_Creatures[Utility.Random( m_Creatures.Length )] ) as Mobile;

							Point3D loc = ( GetSpawnPosition() );
												
							m.MoveToWorld( loc, map );
							Effects.SendLocationParticles( EffectItem.Create( m.Location, m.Map, EffectItem.DefaultDuration ), 0x3709, 10, 30, 5052 );
						
							m_LastUse = DateTime.Now;
							m_NextUse = DateTime.Now + m_Delay;
						}
						else if ( m_NextUse > DateTime.Now )
						{	
							PublicOverheadMessage( MessageType.Regular, 0x3B2, 500760 ); // The brazier fizzes and pops, but nothing seems to happen.
						}
					}
					catch
					{ }
			}
			else
			{	
				from.SendLocalizedMessage( 500446 ); // That is too far away.
			}
		}
		
		private void TalkAgain()
		{
			if( m_NextUse <= DateTime.Now )
				m_CanTalk = true;
		}
		        				
		public Point3D GetSpawnPosition()
		{
			m_SpawnRange = 6;
			Map map = Map;
				
			int x = Location.X + (Utility.Random( (m_SpawnRange * 2) + 1 ) - m_SpawnRange);
			int y = Location.Y + (Utility.Random( (m_SpawnRange * 2) + 1 ) - m_SpawnRange);
			
			return new Point3D( x, y, this.Z );
		}
				
		public override bool HandlesOnMovement{ get{ return true; } }
		
		public override void OnMovement(Mobile m, Point3D oldLocation) 
		{   			
			if ( m.InRange( this, 1 ) && m.Player && m_NextUse <= DateTime.Now && !( m.AccessLevel > AccessLevel.Player && m.Hidden ) && m_CanTalk )
			{
				PublicOverheadMessage( MessageType.Regular, 0x3B2, 500761 ); // Heed this warning well, and use this brazier at your own peril.
				base.OnMovement( m, oldLocation );
				
				m_Timer = Timer.DelayCall( TimeSpan.FromMinutes( 15 ), new TimerCallback( TalkAgain ) );
			}
		}
	
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			
			writer.Write( (int) 0 ); // version
			
			writer.Write( (bool) m_CanTalk );
			
			writer.Write( (DateTime)m_LastUse );
			writer.Write( (DateTime)m_NextUse );
			writer.Write( (int)m_SpawnRange );
			writer.Write( (TimeSpan)m_Delay );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			
			int version = reader.ReadInt();
				m_CanTalk = reader.ReadBool();
				m_LastUse = reader.ReadDateTime();
				m_NextUse = reader.ReadDateTime();
				m_SpawnRange = reader.ReadInt();
				m_Delay = reader.ReadTimeSpan();
		}
	}
}
