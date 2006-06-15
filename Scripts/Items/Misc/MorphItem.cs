using System;

namespace Server.Items
{
	public class MorphItem : Item
	{
		private int m_InactiveItemID;
		private int m_ActiveItemID;
		private int m_InRange;
		private int m_OutRange;

		[CommandProperty( AccessLevel.GameMaster )]
		public int InactiveItemID
		{
			get{ return m_InactiveItemID; }
			set{ m_InactiveItemID = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int ActiveItemID
		{
			get{ return m_ActiveItemID; }
			set{ m_ActiveItemID = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int InRange
		{
			get{ return m_InRange; }
			set{ if ( value > 18 ) value = 18; m_InRange = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int OutRange
		{
			get{ return m_OutRange; }
			set{ if ( value > 18 ) value = 18; m_OutRange = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int CurrentRange{ get{ return ItemID == InactiveItemID ? InRange : OutRange; } }

		[Constructable]
		public MorphItem( int inactiveItemID, int activeItemID, int range ) : this( inactiveItemID, activeItemID, range, range )
		{
		}

		[Constructable]
		public MorphItem( int inactiveItemID, int activeItemID, int inRange, int outRange ) : base( inactiveItemID )
		{
			Movable = false;

			InactiveItemID = inactiveItemID;
			ActiveItemID = activeItemID;
			InRange = inRange;
			OutRange = outRange;
		}

		public MorphItem( Serial serial ) : base( serial )
		{
		}

		public override bool HandlesOnMovement{ get{ return true; } }

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			if ( Utility.InRange( m.Location, Location, CurrentRange ) || Utility.InRange( oldLocation, Location, CurrentRange ) )
				Refresh();
		}

		public override void OnMapChange()
		{
			if ( !Deleted )
				Refresh();
		}

		public override void OnLocationChange( Point3D oldLoc )
		{
			if ( !Deleted )
				Refresh();
		}

		public void Refresh()
		{
			bool found = false;

			foreach ( Mobile mob in GetMobilesInRange( CurrentRange ) )
			{
				if ( mob.Hidden && mob.AccessLevel > AccessLevel.Player )
					continue;

				found = true;
				break;
			}

			if ( found )
				ItemID = ActiveItemID;
			else
				ItemID = InactiveItemID;

			Visible = ( ItemID != 0x1 );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( (int) m_OutRange );

			writer.Write( (int) m_InactiveItemID );
			writer.Write( (int) m_ActiveItemID );
			writer.Write( (int) m_InRange );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_OutRange = reader.ReadInt();
					goto case 0;
				}
				case 0:
				{
					m_InactiveItemID = reader.ReadInt();
					m_ActiveItemID = reader.ReadInt();
					m_InRange = reader.ReadInt();

					if ( version < 1 )
						m_OutRange = m_InRange;

					break;
				}
			}

			Timer.DelayCall( TimeSpan.Zero, new TimerCallback( Refresh ) );
		}
	}
}