using System;
using Server;
using Server.Multis;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Items
{
	public abstract class BaseWindChimes : Item
	{
		private bool m_TurnedOn;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool TurnedOn
		{
			get{ return m_TurnedOn; }
			set{ m_TurnedOn = value; InvalidateProperties(); }
		}

		public BaseWindChimes( int itemID ) : base( itemID )
		{
		}

		private static int[] m_Sounds = new int[] { 0x505, 0x506, 0x507 };

		public static int[] Sounds
		{
			get{ return m_Sounds; }
		}

		public override bool HandlesOnMovement{ get{ return m_TurnedOn && IsLockedDown; } }

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			if ( m_TurnedOn && IsLockedDown && (!m.Hidden || m.AccessLevel == AccessLevel.Player) && Utility.InRange( m.Location, this.Location, 2 ) && !Utility.InRange( oldLocation, this.Location, 2 ) )
				Effects.PlaySound( this.Location, this.Map, m_Sounds[Utility.Random( m_Sounds.Length )] );

			base.OnMovement( m, oldLocation );
		}

		public BaseWindChimes( Serial serial ) : base( serial )
		{
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_TurnedOn )
				list.Add( 502695 ); // turned on
			else
				list.Add( 502696 ); // turned off
		}

		public bool IsOwner( Mobile mob )
		{
			BaseHouse house = BaseHouse.FindHouseAt( this );

			return ( house != null && house.IsOwner( mob ) );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( IsOwner( from ) )
			{
				OnOffGump onOffGump = new OnOffGump( this );
				from.SendGump( onOffGump );
			}
			else
			{
				from.SendLocalizedMessage( 502691 ); // You must be the owner to use this.
			}
		}

		private class OnOffGump : Gump
		{
			private BaseWindChimes m_Chimes;

			public OnOffGump( BaseWindChimes chimes ) : base( 150, 200 )
			{
				m_Chimes = chimes;

				AddBackground( 0, 0, 300, 150, 0xA28 );
				AddHtmlLocalized( 45, 20, 300, 35, chimes.TurnedOn ? 1011035 : 1011034, false, false ); // [De]Activate this item
				AddButton( 40, 53, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0 );
				AddHtmlLocalized( 80, 55, 65, 35, 1011036, false, false ); // OKAY
				AddButton( 150, 53, 0xFA5, 0xFA7, 0, GumpButtonType.Reply, 0 );
				AddHtmlLocalized( 190, 55, 100, 35, 1011012, false, false ); // CANCEL
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				Mobile from = sender.Mobile;

				if ( info.ButtonID == 1 )
				{
					bool newValue = !m_Chimes.TurnedOn;

					m_Chimes.TurnedOn = newValue;

					if ( newValue && !m_Chimes.IsLockedDown )
						from.SendLocalizedMessage( 502693 ); // Remember, this only works when locked down.
				}
				else
				{
					from.SendLocalizedMessage( 502694 ); // Cancelled action.
				}
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (bool) m_TurnedOn );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_TurnedOn = reader.ReadBool();
					break;
				}
			}
		}
	}

	public class WindChimes : BaseWindChimes
	{
		public override int LabelNumber{ get{ return 1030290; } }

		[Constructable]
		public WindChimes() : base( 0x2832 )
		{
		}

		public WindChimes( Serial serial ) : base( serial )
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

	public class FancyWindChimes : BaseWindChimes
	{
		public override int LabelNumber{ get{ return 1030291; } }

		[Constructable]
		public FancyWindChimes() : base( 0x2833 )
		{
		}

		public FancyWindChimes( Serial serial ) : base( serial )
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