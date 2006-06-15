using System;

namespace Server.Items
{
	public abstract class BaseTrap : Item
	{
		public virtual bool PassivelyTriggered{ get{ return false; } }
		public virtual TimeSpan PassiveTriggerDelay{ get{ return TimeSpan.Zero; } }
		public virtual int PassiveTriggerRange{ get{ return -1; } }
		public virtual TimeSpan ResetDelay{ get{ return TimeSpan.Zero; } }

		private DateTime m_NextPassiveTrigger, m_NextActiveTrigger;

		public virtual void OnTrigger( Mobile from )
		{
		}

		public override bool HandlesOnMovement{ get{ return true; } } // Tell the core that we implement OnMovement

		public virtual int GetEffectHue()
		{
			int hue = this.Hue & 0x3FFF;

			if ( hue < 2 )
				return 0;

			return hue - 1;
		}

		public bool CheckRange( Point3D loc, Point3D oldLoc, int range )
		{
			return CheckRange( loc, range ) && !CheckRange( oldLoc, range );
		}

		public bool CheckRange( Point3D loc, int range )
		{
			return ( (this.Z + 8) >= loc.Z && (loc.Z + 16) > this.Z )
				&& Utility.InRange( GetWorldLocation(), loc, range );
		}

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			base.OnMovement( m, oldLocation );

			if ( m.Location == oldLocation )
				return;

			if ( CheckRange( m.Location, oldLocation, 0 ) && DateTime.Now >= m_NextActiveTrigger )
			{
				m_NextActiveTrigger = m_NextPassiveTrigger = DateTime.Now + ResetDelay;

				OnTrigger( m );
			}
			else if ( PassivelyTriggered && CheckRange( m.Location, oldLocation, PassiveTriggerRange ) && DateTime.Now >= m_NextPassiveTrigger )
			{
				m_NextPassiveTrigger = DateTime.Now + PassiveTriggerDelay;

				OnTrigger( m );
			}
		}

		public BaseTrap( int itemID ) : base( itemID )
		{
			Movable = false;
		}

		public BaseTrap( Serial serial ) : base( serial )
		{
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