using System;
using Server;
using Server.Items;

namespace Server.Items
{	
	public class BaseFish : Item
	{
		private Timer m_Timer;		
		
		[CommandProperty( AccessLevel.GameMaster )]
		public bool Dead
		{
			get{ return ItemID == 0x3B0C; }
		}
					
		[Constructable]
		public BaseFish( int itemID ) : base( itemID )
		{
			StartTimer();
		}

		public BaseFish( Serial serial ) : base( serial )
		{		
		}
		
		public virtual void StartTimer()
		{
			if ( m_Timer != null )
				m_Timer.Stop();
						
			m_Timer = Timer.DelayCall( TimeSpan.FromMinutes( 5 ), new TimerCallback( Kill ) );
		}
		
		public virtual void StopTimer()
		{
			if ( m_Timer != null )
				m_Timer.Stop();
				
			m_Timer = null;
		}
		
		public virtual void Kill()
		{
			ItemID = 0x3B0C;
			StopTimer();
			
			InvalidateProperties();
		}
		
		public override bool DropToItem( Mobile from, Item target, Point3D p )
		{				
			if ( target is FishBowl || target is Aquarium )
			{
				if ( base.DropToItem( from, target, p ) )
				{
					StopTimer();		
						
					return true;
				}
			}	
			
			return base.DropToItem( from, target, p );
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( !Dead && ItemID > 0x3B0F )
				list.Add( 1074422 ); // A very unusual live aquarium creature
			else if ( !Dead && Hue > 0 )
				list.Add( 1074423 ); // A live aquarium creature of unusual color
			else if ( !Dead )
				list.Add( 1073622 ); // A live aquarium creature
			else if ( Dead && ItemID > 0x3B0F )
				list.Add( 1074424 ); // A very unusual dead aquarium creature
			else if ( Dead && Hue > 0 )
				list.Add( 1074425 ); // A dead aquarium creature of unusual color			
			else if ( Dead )
				list.Add( 1073623 ); // A dead aquarium creature
			
			if ( !Dead && m_Timer != null )
				list.Add( 1074507 ); // Gasping for air
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
			
			if ( !( Parent is Aquarium ) && !( Parent is FishBowl ) )
				StartTimer();
		}
	}
}
