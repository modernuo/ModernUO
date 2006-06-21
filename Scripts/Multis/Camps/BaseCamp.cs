using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Multis
{
	public abstract class BaseCamp : BaseMulti
	{
		private List<Item> m_Items;
		private List<Mobile> m_Mobiles;
		private DateTime m_DecayTime;
		private Timer m_DecayTimer;

		public virtual int EventRange{ get{ return 10; } }
		public virtual TimeSpan DecayDelay{ get{ return TimeSpan.FromMinutes( 30.0 ); } }

		public BaseCamp( int multiID ) : base( multiID | 0x4000 )
		{
			m_Items = new List<Item>();
			m_Mobiles = new List<Mobile>();
			RefreshDecay( true );

			Timer.DelayCall( TimeSpan.Zero, new TimerCallback( CheckAddComponents ) );
		}

		public void CheckAddComponents()
		{
			if ( Deleted )
				return;
			
			AddComponents();
		}

		public virtual void AddComponents()
		{
		}

		public virtual void RefreshDecay( bool setDecayTime )
		{
			if ( Deleted )
				return;

			if ( m_DecayTimer != null )
				m_DecayTimer.Stop();

			if ( setDecayTime )
				m_DecayTime = DateTime.Now + DecayDelay;

			m_DecayTimer = Timer.DelayCall( DecayDelay, new TimerCallback( Delete ) );
		}

		public virtual void AddItem( Item item, int xOffset, int yOffset, int zOffset )
		{
			m_Items.Add( item );

			item.MoveToWorld( new Point3D( X + xOffset, Y + yOffset, Z + zOffset ), Map );
		}

		public virtual void AddMobile( Mobile m, int wanderRange, int xOffset, int yOffset, int zOffset )
		{
			m_Mobiles.Add( m );

			Point3D loc = new Point3D( X + xOffset, Y + yOffset, Z + zOffset );
			BaseCreature bc = m as BaseCreature;

			if ( bc != null )
			{
				bc.RangeHome = wanderRange; 
				bc.Home = loc; 
			}

			if ( m is BaseVendor || m is Banker )
				m.Direction = Direction.South;

			m.MoveToWorld( loc, this.Map );
		}

		public virtual void OnEnter( Mobile m )
		{
			RefreshDecay( true );
		}

		public virtual void OnExit( Mobile m )
		{
			RefreshDecay( true );
		}

		public override bool HandlesOnMovement{ get{ return true; } }

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			bool inOldRange = Utility.InRange( oldLocation, Location, EventRange );
			bool inNewRange = Utility.InRange( m.Location, Location, EventRange );

			if ( inNewRange && !inOldRange )
				OnEnter( m );
			else if ( inOldRange && !inNewRange )
				OnExit( m );
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			for ( int i = 0; i < m_Items.Count; ++i )
				m_Items[i].Delete();

			for ( int i = 0; i < m_Mobiles.Count; ++i )
				m_Mobiles[i].Delete();

			m_Items.Clear();
			m_Mobiles.Clear();
		}

		public BaseCamp( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_Items, true );
			writer.Write( m_Mobiles, true );
			writer.WriteDeltaTime( m_DecayTime );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Items = reader.ReadStrongItemList();
					m_Mobiles = reader.ReadStrongMobileList();
					m_DecayTime = reader.ReadDeltaTime();

					RefreshDecay( false );

					break;
				}
			}
		}
	}
}