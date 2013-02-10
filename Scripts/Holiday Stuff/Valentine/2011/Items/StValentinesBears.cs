using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Items
{
	public abstract class StValentinesBear : Item
	{
		public override string DefaultName
		{
			get
			{
				if ( m_Owner != null )
					return String.Format( "{0}'s St. Valentine Bear", m_Owner );
				else
					return "St. Valentine Bear";
			}
		}

		private string m_Owner;
		private string m_Line1;
		private string m_Line2;
		private string m_Line3;

		private DateTime m_EditLimit;

		[CommandProperty( AccessLevel.GameMaster )]
		public string Owner
		{
			get { return m_Owner; }
			set { m_Owner = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public string Line1
		{
			get { return m_Line1; }
			set { m_Line1 = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public string Line2
		{
			get { return m_Line2; }
			set { m_Line2 = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public string Line3
		{
			get { return m_Line3; }
			set { m_Line3 = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime EditLimit
		{
			get { return m_EditLimit; }
			set { m_EditLimit = value; }
		}

		public bool IsSigned
		{
			get { return ( m_Line1 != null || m_Line2 != null || m_Line3 != null ); }
		}

		public bool CanSign
		{
			get { return ( !IsSigned || DateTime.Now <= m_EditLimit ); }
		}

		public StValentinesBear( int itemid, string name )
			: base( itemid )
		{
			m_Owner = name;
			LootType = LootType.Blessed;
		}

		public override void AddNameProperty( ObjectPropertyList list )
		{
			if ( m_Owner != null )
				list.Add( 1150295, m_Owner ); // ~1_NAME~'s St. Valentine Bear
			else
				list.Add( 1150294 ); // St. Valentine Bear

			AddLine( list, 1150301, m_Line1 ); // [ ~1_LINE0~ ]
			AddLine( list, 1150302, m_Line2 ); // [ ~1_LINE1~ ]
			AddLine( list, 1150303, m_Line3 ); // [ ~1_LINE2~ ]
		}

		private static void AddLine( ObjectPropertyList list, int cliloc, string line )
		{
			if ( line != null )
				list.Add( cliloc, line );
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			ShowLine( from, 1150301, m_Line1 ); // [ ~1_LINE0~ ]
			ShowLine( from, 1150302, m_Line2 ); // [ ~1_LINE1~ ]
			ShowLine( from, 1150303, m_Line3 ); // [ ~1_LINE2~ ]
		}

		private void ShowLine( Mobile from, int cliloc, string line )
		{
			if ( line != null )
				LabelTo( from, cliloc, line );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !CupidsArrow.CheckSeason( from ) || !CanSign )
				return;

			if ( !IsChildOf( from.Backpack ) )
			{
				from.SendLocalizedMessage( 1080063 ); // This must be in your backpack to use it.
				return;
			}

			from.SendGump( new InternalGump( this ) );
		}

		public StValentinesBear( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );

			writer.Write( m_Owner );
			writer.Write( m_Line1 );
			writer.Write( m_Line2 );
			writer.Write( m_Line3 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Owner = Utility.Intern( reader.ReadString() );
			m_Line1 = Utility.Intern( reader.ReadString() );
			m_Line2 = Utility.Intern( reader.ReadString() );
			m_Line3 = Utility.Intern( reader.ReadString() );
		}

		private class InternalGump : Gump
		{
			private StValentinesBear m_Bear;

			public InternalGump( StValentinesBear bear )
				: base( 50, 50 )
			{
				m_Bear = bear;

				AddPage( 0 );
				AddBackground( 0, 0, 420, 320, 9300 );
				AddHtml( 10, 10, 400, 21, "<CENTER>St. Valentine Bear</CENTER>", false, false );
				AddHtmlLocalized( 10, 40, 400, 75, 1150293, 0, false, false ); // Enter up to three lines of personalized greeting for your St. Valentine Bear. You many enter up to 25 characters per line. Once you enter text, you will only be able to correct mistakes for 10 minutes.

				AddHtmlLocalized( 10, 129, 400, 21, 1150296, 0, false, false ); // Line 1:
				AddBackground( 10, 150, 400, 24, 9350 );
				AddTextEntry( 15, 152, 390, 20, 0, 0, "", 25 );

				AddHtmlLocalized( 10, 179, 400, 21, 1150297, 0, false, false ); // Line 2:
				AddBackground( 10, 200, 400, 24, 9350 );
				AddTextEntry( 15, 202, 390, 20, 0, 1, "", 25 );

				AddHtmlLocalized( 10, 229, 400, 21, 1150298, 0, false, false ); // Line 3:
				AddBackground( 10, 250, 400, 24, 9350 );
				AddTextEntry( 15, 252, 390, 20, 0, 2, "", 25 );

				AddButton( 15, 285, 242, 241, 0, GumpButtonType.Reply, 0 );
				AddButton( 335, 285, 247, 248, 1, GumpButtonType.Reply, 0 );
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				Mobile from = sender.Mobile;

				if ( m_Bear.Deleted || !m_Bear.IsChildOf( from.Backpack ) || !m_Bear.CanSign || info.ButtonID != 1 )
					return;

				string line1 = GetLine( info, 0 );
				string line2 = GetLine( info, 1 );
				string line3 = GetLine( info, 2 );

				if ( string.IsNullOrEmpty( line1 )
					|| string.IsNullOrEmpty( line2 )
					|| string.IsNullOrEmpty( line3 ) )
				{
					from.SendMessage( "Lines cannot be left blank." );
					return;
				}
				else if ( line1.Length > 25
					|| line2.Length > 25
					|| line3.Length > 25 )
				{
					from.SendMessage( "Lines may not exceed 25 characters." );
					return;
				}

				if ( !m_Bear.IsSigned )
					m_Bear.EditLimit = DateTime.Now + TimeSpan.FromMinutes( 10 );

				m_Bear.Line1 = Utility.FixHtml( line1 );
				m_Bear.Line2 = Utility.FixHtml( line2 );
				m_Bear.Line3 = Utility.FixHtml( line3 );

				from.SendMessage( "You add the personalized greeting to your St. Valentine Bear." );
			}

			private static string GetLine( RelayInfo info, int idx )
			{
				TextRelay tr = info.GetTextEntry( idx );

				return ( tr == null ) ? null : tr.Text;
			}
		}
	}

	[FlipableAttribute( 0x48E0, 0x48E1 )]
	public class StValentinesPanda : StValentinesBear
	{
		[Constructable]
		public StValentinesPanda()
			: this( null )
		{
		}

		[Constructable]
		public StValentinesPanda( string name )
			: base( 0x48E0, name )
		{
		}

		public StValentinesPanda( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	[FlipableAttribute( 0x48E2, 0x48E3 )]
	public class StValentinesPolarBear : StValentinesBear
	{
		[Constructable]
		public StValentinesPolarBear()
			: this( null )
		{
		}

		[Constructable]
		public StValentinesPolarBear( string name )
			: base( 0x48E2, name )
		{
		}

		public StValentinesPolarBear( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
