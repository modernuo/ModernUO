/***************************************************************************
 *                              ItemListMenu.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using Server.Network;

namespace Server.Menus.ItemLists
{
	public class ItemListEntry
	{
		public string Name { get; }

		public int ItemID { get; }

		public int Hue { get; }

		public ItemListEntry( string name, int itemID ) : this( name, itemID, 0 )
		{
		}

		public ItemListEntry( string name, int itemID, int hue )
		{
			Name = name;
			ItemID = itemID;
			Hue = hue;
		}
	}

	public class ItemListMenu : IMenu
	{
		private int m_Serial;
		private static int m_NextSerial;

		int IMenu.Serial => m_Serial;

		int IMenu.EntryLength => Entries.Length;

		public string Question { get; }

		public ItemListEntry[] Entries { get; set; }

		public ItemListMenu( string question, ItemListEntry[] entries )
		{
			Question = question;
			Entries = entries;

			do
			{
				m_Serial = m_NextSerial++;
				m_Serial &= 0x7FFFFFFF;
			} while ( m_Serial == 0 );

			m_Serial = (int)((uint)m_Serial | 0x80000000);
		}

		public virtual void OnCancel( NetState state )
		{
		}

		public virtual void OnResponse( NetState state, int index )
		{
		}

		public void SendTo( NetState state )
		{
			state.AddMenu( this );
			state.Send( new DisplayItemListMenu( this ) );
		}
	}
}