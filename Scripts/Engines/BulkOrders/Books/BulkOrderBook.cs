using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Multis;
using Server.Prompts;
using Server.Mobiles;
using Server.ContextMenus;

namespace Server.Engines.BulkOrders
{
	public class BulkOrderBook : Item, ISecurable
	{
		private ArrayList m_Entries;
		private BOBFilter m_Filter;
		private string m_BookName;
		private SecureLevel m_Level;

		[CommandProperty( AccessLevel.GameMaster )]
		public string BookName
		{
			get{ return m_BookName; }
			set{ m_BookName = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public SecureLevel Level
		{
			get{ return m_Level; }
			set{ m_Level = value; }
		}

		public ArrayList Entries
		{
			get{ return m_Entries; }
		}

		public BOBFilter Filter
		{
			get{ return m_Filter; }
		}

		[Constructable]
		public BulkOrderBook() : base( 0x2259 )
		{
			Weight = 1.0;
			LootType = LootType.Blessed;

			m_Entries = new ArrayList();
			m_Filter = new BOBFilter();

			m_Level = SecureLevel.CoOwners;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.InRange( GetWorldLocation(), 2 ) )
				from.LocalOverheadMessage( Network.MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
			else if ( m_Entries.Count == 0 )
				from.SendLocalizedMessage( 1062381 ); // The book is empty.
			else if ( from is PlayerMobile )
				from.SendGump( new BOBGump( (PlayerMobile)from, this ) );
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			if ( dropped is LargeBOD )
			{
				if ( !IsChildOf( from.Backpack ) )
				{
					from.SendLocalizedMessage( 1062385 ); // You must have the book in your backpack to add deeds to it.
					return false;
				}
				else if ( m_Entries.Count < 500 )
				{
					m_Entries.Add( new BOBLargeEntry( (LargeBOD)dropped ) );
					InvalidateProperties();

					from.SendLocalizedMessage( 1062386 ); // Deed added to book.

					if ( from is PlayerMobile )
						from.SendGump( new BOBGump( (PlayerMobile)from, this ) );

					dropped.Delete();
					return true;
				}
				else
				{
					from.SendLocalizedMessage( 1062387 ); // The book is full of deeds.
					return false;
				}
			}
			else if ( dropped is SmallBOD )
			{
				if ( !IsChildOf( from.Backpack ) )
				{
					from.SendLocalizedMessage( 1062385 ); // You must have the book in your backpack to add deeds to it.
					return false;
				}
				else if ( m_Entries.Count < 500 )
				{
					m_Entries.Add( new BOBSmallEntry( (SmallBOD)dropped ) );
					InvalidateProperties();

					from.SendLocalizedMessage( 1062386 ); // Deed added to book.

					if ( from is PlayerMobile )
						from.SendGump( new BOBGump( (PlayerMobile)from, this ) );

					dropped.Delete();
					return true;
				}
				else
				{
					from.SendLocalizedMessage( 1062387 ); // The book is full of deeds.
					return false;
				}
			}

			from.SendLocalizedMessage( 1062388 ); // That is not a bulk order deed.
			return false;
		}

		public BulkOrderBook( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( (int) m_Level );

			writer.Write( m_BookName );

			m_Filter.Serialize( writer );

			writer.WriteEncodedInt( (int) m_Entries.Count );

			for ( int i = 0; i < m_Entries.Count; ++i )
			{
				object obj = m_Entries[i];

				if ( obj is BOBLargeEntry )
				{
					writer.WriteEncodedInt( 0 );
					((BOBLargeEntry)obj).Serialize( writer );
				}
				else if ( obj is BOBSmallEntry )
				{
					writer.WriteEncodedInt( 1 );
					((BOBSmallEntry)obj).Serialize( writer );
				}
				else
				{
					writer.WriteEncodedInt( -1 );
				}
			}
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_Level = (SecureLevel)reader.ReadInt();
					goto case 0;
				}
				case 0:
				{
					m_BookName = reader.ReadString();

					m_Filter = new BOBFilter( reader );

					int count = reader.ReadEncodedInt();

					m_Entries = new ArrayList( count );

					for ( int i = 0; i < count; ++i )
					{
						int v = reader.ReadEncodedInt();

						switch ( v )
						{
							case 0: m_Entries.Add( new BOBLargeEntry( reader ) ); break;
							case 1: m_Entries.Add( new BOBSmallEntry( reader ) ); break;
						}
					}

					break;
				}
			}
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1062344, m_Entries.Count.ToString() ); // Deeds in book: ~1_val~

			if ( m_BookName != null && m_BookName.Length > 0 )
				list.Add( 1062481, m_BookName ); // Book Name: ~1_val~
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			if ( from.CheckAlive() && IsChildOf( from.Backpack ) )
				list.Add( new NameBookEntry( from, this ) );

			SetSecureLevelEntry.AddTo( from, this, list );
		}

		private class NameBookEntry : ContextMenuEntry
		{
			private Mobile m_From;
			private BulkOrderBook m_Book;

			public NameBookEntry( Mobile from, BulkOrderBook book ) : base( 6216 )
			{
				m_From = from;
				m_Book = book;
			}

			public override void OnClick()
			{
				if ( m_From.CheckAlive() && m_Book.IsChildOf( m_From.Backpack ) )
				{
					m_From.Prompt = new NameBookPrompt( m_Book );
					m_From.SendLocalizedMessage( 1062479 ); // Type in the new name of the book:
				}
			}
		}

		private class NameBookPrompt : Prompt
		{
			private BulkOrderBook m_Book;

			public NameBookPrompt( BulkOrderBook book )
			{
				m_Book = book;
			}

			public override void OnResponse( Mobile from, string text )
			{
				if ( text.Length > 40 )
					text = text.Substring( 0, 40 );

				if ( from.CheckAlive() && m_Book.IsChildOf( from.Backpack ) )
				{
					m_Book.BookName = Utility.FixHtml( text.Trim() );

					from.SendLocalizedMessage( 1062480 ); // The bulk order book's name has been changed.
				}
			}

			public override void OnCancel( Mobile from )
			{
			}
		}
	}
}