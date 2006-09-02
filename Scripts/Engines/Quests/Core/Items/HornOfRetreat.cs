using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.Quests;
using Server.Engines.Quests.Necro;

namespace Server.Engines.Quests
{
	public class HornOfRetreat : Item
	{
		private Point3D m_DestLoc;
		private Map m_DestMap;
		private int m_Charges;

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D DestLoc
		{
			get{ return m_DestLoc; }
			set{ m_DestLoc = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Map DestMap
		{
			get{ return m_DestMap; }
			set{ m_DestMap = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Charges
		{
			get{ return m_Charges; }
			set{ m_Charges = value; InvalidateProperties(); }
		}

		public override int LabelNumber{ get{ return 1049117; } } // Horn of Retreat

		[Constructable]
		public HornOfRetreat() : base( 0xFC4 )
		{
			Hue = 0x482;
			Weight = 1.0;
			Charges = 10;
		}

		public virtual bool ValidateUse( Mobile from )
		{
			return true;
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1060741, m_Charges.ToString() ); // charges: ~1_val~
		}

		private Timer m_PlayTimer;

		public override void OnDoubleClick( Mobile from )
		{
			if ( IsChildOf( from.Backpack ) )
			{
				if ( !ValidateUse( from ) )
				{
					SendLocalizedMessageTo( from, 500309 ); // Nothing Happens.
				}
				else if( Core.ML && from.Map != Map.Trammel && from.Map != Map.Malas )
				{
					from.SendLocalizedMessage( 1076154 ); // You can only use this in Trammel and Malas.
				}
				else if( m_PlayTimer != null )
				{
					SendLocalizedMessageTo( from, 1042144 ); // This is currently in use.
				}
				else if( Charges > 0 )
				{
					from.Animate( 34, 7, 1, true, false, 0 );
					from.PlaySound( 0xFF );
					from.SendLocalizedMessage( 1049115 ); // You play the horn and a sense of peace overcomes you...

					--Charges;

					m_PlayTimer = Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), new TimerStateCallback( PlayTimer_Callback ), from );
				}
				else
				{
					SendLocalizedMessageTo( from, 1042544 ); // This item is out of charges.
				}
			}
			else
			{
				SendLocalizedMessageTo( from, 1042001 ); // That must be in your pack for you to use it.
			}
		}

		public virtual void PlayTimer_Callback( object state )
		{
			Mobile from = (Mobile)state;

			m_PlayTimer = null;

			HornOfRetreatMoongate gate = new HornOfRetreatMoongate( this.DestLoc, this.DestMap, from, this.Hue );

			gate.MoveToWorld( from.Location, from.Map );

			from.PlaySound( 0x20E );

			gate.SendLocalizedMessageTo( from, 1049102, from.Name ); // Quickly ~1_NAME~! Onward through the gate!
		}

		public HornOfRetreat( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_DestLoc );
			writer.Write( m_DestMap );
			writer.Write( m_Charges );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_DestLoc = reader.ReadPoint3D();
					m_DestMap = reader.ReadMap();
					m_Charges = reader.ReadInt();
					break;
				}
			}
		}
	}

	public class HornOfRetreatMoongate : Moongate
	{
		public override int LabelNumber{ get{ return 1049114; } } // Sanctuary Gate

		private Mobile m_Caster;

		public HornOfRetreatMoongate( Point3D destLoc, Map destMap, Mobile caster, int hue )
		{
			m_Caster = caster;

			Target = destLoc;
			TargetMap = destMap;

			Hue = hue;
			Light = LightType.Circle300;

			Dispellable = false;

			Timer.DelayCall( TimeSpan.FromSeconds( 10.0 ), new TimerCallback( Delete ) );
		}

		public override void BeginConfirmation( Mobile from )
		{
			EndConfirmation( from );
		}

		public override void UseGate( Mobile m )
		{
			if ( m.Region.IsPartOf( typeof( Regions.Jail ) ) )
			{
				m.SendLocalizedMessage( 1042632 ); // You'll need a better jailbreak plan then that!
			}
			else if ( m == m_Caster )
			{
				base.UseGate( m );
				Delete();
			}
		}

		public HornOfRetreatMoongate( Serial serial ) : base( serial )
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

			Delete();
		}
	}
}