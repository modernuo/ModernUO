using System;
using Server;

namespace Server.Items
{
	[Flipable( 0x1EC0, 0x1EC3 )]
	public class PickpocketDip : AddonComponent
	{
		private double m_MinSkill;
		private double m_MaxSkill;

		private Timer m_Timer;

		[CommandProperty( AccessLevel.GameMaster )]
		public double MinSkill
		{
			get{ return m_MinSkill; }
			set{ m_MinSkill = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public double MaxSkill
		{
			get{ return m_MaxSkill; }
			set{ m_MaxSkill = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Swinging
		{
			get{ return ( m_Timer != null ); } 
		}

		public PickpocketDip( int itemID ) : base( itemID )
		{
			m_MinSkill = -25.0;
			m_MaxSkill = +25.0;
		}

		public void UpdateItemID()
		{
			int baseItemID = 0x1EC0 + (((ItemID - 0x1EC0) / 3) * 3);

			ItemID = baseItemID + (Swinging ? 1 : 0);
		}

		public void BeginSwing()
		{
			if ( m_Timer != null )
				m_Timer.Stop();

			m_Timer = new InternalTimer( this );
			m_Timer.Start();

			UpdateItemID();
		}

		public void EndSwing()
		{
			if ( m_Timer != null )
				m_Timer.Stop();

			m_Timer = null;

			UpdateItemID();
		}

		public void Use( Mobile from )
		{
			from.Direction = from.GetDirectionTo( GetWorldLocation() );

			Effects.PlaySound( GetWorldLocation(), Map, 0x4F );

			if ( from.CheckSkill( SkillName.Stealing, m_MinSkill, m_MaxSkill ) )
			{
				SendLocalizedMessageTo( from, 501834 ); // You successfully avoid disturbing the dip while searching it.
			}
			else
			{
				Effects.PlaySound( GetWorldLocation(), Map, 0x390 );

				BeginSwing();
				ProcessDelta();
				SendLocalizedMessageTo( from, 501831 ); // You carelessly bump the dip and start it swinging.
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.InRange( GetWorldLocation(), 1 ) )
				SendLocalizedMessageTo( from, 501816 ); // You are too far away to do that.
			else if ( Swinging )
				SendLocalizedMessageTo( from, 501815 ); // You have to wait until it stops swinging.
			else if ( from.Skills[SkillName.Stealing].Base >= m_MaxSkill )
				SendLocalizedMessageTo( from, 501830 ); // Your ability to steal cannot improve any further by simply practicing on a dummy.
			else if ( from.Mounted )
				SendLocalizedMessageTo( from, 501829 ); // You can't practice on this while on a mount.
			else
				Use( from );
		}

		public PickpocketDip( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );

			writer.Write( m_MinSkill );
			writer.Write( m_MaxSkill );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_MinSkill = reader.ReadDouble();
					m_MaxSkill = reader.ReadDouble();

					if ( m_MinSkill == 0.0 && m_MaxSkill == 30.0 )
					{
						m_MinSkill = -25.0;
						m_MaxSkill = +25.0;
					}

					break;
				}
			}

			UpdateItemID();
		}

		private class InternalTimer : Timer
		{
			private PickpocketDip m_Dip;

			public InternalTimer( PickpocketDip dip ) : base( TimeSpan.FromSeconds( 3.0 ) )
			{
				m_Dip = dip;
				Priority = TimerPriority.FiftyMS;
			}

			protected override void OnTick()
			{
				m_Dip.EndSwing();
			}
		}
	}

	public class PickpocketDipEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new PickpocketDipEastDeed(); } }

		[Constructable]
		public PickpocketDipEastAddon()
		{
			AddComponent( new PickpocketDip( 0x1EC3 ), 0, 0, 0 );
		}

		public PickpocketDipEastAddon( Serial serial ) : base( serial )
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

	public class PickpocketDipEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new PickpocketDipEastAddon(); } }
		public override int LabelNumber{ get{ return 1044337; } } // pickpocket dip (east)

		[Constructable]
		public PickpocketDipEastDeed()
		{
		}

		public PickpocketDipEastDeed( Serial serial ) : base( serial )
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

	public class PickpocketDipSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new PickpocketDipSouthDeed(); } }

		[Constructable]
		public PickpocketDipSouthAddon()
		{
			AddComponent( new PickpocketDip( 0x1EC0 ), 0, 0, 0 );
		}

		public PickpocketDipSouthAddon( Serial serial ) : base( serial )
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

	public class PickpocketDipSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new PickpocketDipSouthAddon(); } }
		public override int LabelNumber{ get{ return 1044338; } } // pickpocket dip (south)

		[Constructable]
		public PickpocketDipSouthDeed()
		{
		}

		public PickpocketDipSouthDeed( Serial serial ) : base( serial )
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