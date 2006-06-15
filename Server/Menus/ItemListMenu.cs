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

using System;
using Server.Network;

namespace Server.Menus.ItemLists
{
	public class ItemListEntry
	{
		private string m_Name;
		private int m_ItemID;
		private int m_Hue;

		public string Name
		{
			get
			{
				return m_Name;
			}
		}

		public int ItemID
		{
			get
			{
				return m_ItemID;
			}
		}

		public int Hue
		{
			get
			{
				return m_Hue;
			}
		}

		public ItemListEntry( string name, int itemID ) : this( name, itemID, 0 )
		{
		}

		public ItemListEntry( string name, int itemID, int hue )
		{
			m_Name = name;
			m_ItemID = itemID;
			m_Hue = hue;
		}
	}

	public class ItemListMenu : IMenu
	{
		private string m_Question;
		private ItemListEntry[] m_Entries;

		private int m_Serial;
		private static int m_NextSerial;

		int IMenu.Serial
		{
			get
			{
				return m_Serial;
			}
		}

		int IMenu.EntryLength
		{
			get
			{
				return m_Entries.Length;
			}
		}

		public string Question
		{
			get
			{
				return m_Question;
			}
		}

		public ItemListEntry[] Entries
		{
			get
			{
				return m_Entries;
			}
			set
			{
				m_Entries = value;
			}
		}

		public ItemListMenu( string question, ItemListEntry[] entries )
		{
			m_Question = question;
			m_Entries = entries;

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