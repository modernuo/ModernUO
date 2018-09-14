/***************************************************************************
 *                               BaseVendor.cs
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

using System.Collections.Generic;

namespace Server.Mobiles
{
	public class BuyItemStateComparer : IComparer<BuyItemState>
	{
		public int Compare( BuyItemState l, BuyItemState r )
		{
			if ( l == null && r == null ) return 0;
			if ( l == null ) return -1;
			if ( r == null ) return 1;

			return l.MySerial.CompareTo( r.MySerial );
		}
	}

	public class BuyItemResponse
	{
		private Serial m_Serial;
		private int m_Amount;

		public BuyItemResponse( Serial serial, int amount )
		{
			m_Serial = serial;
			m_Amount = amount;
		}

		public Serial Serial => m_Serial;

		public int Amount => m_Amount;
	}

	public class SellItemResponse
	{
		private Item m_Item;
		private int m_Amount;

		public SellItemResponse( Item i, int amount )
		{
			m_Item = i;
			m_Amount = amount;
		}

		public Item Item => m_Item;

		public int Amount => m_Amount;
	}

	public class SellItemState
	{
		private Item m_Item;
		private int m_Price;
		private string m_Name;

		public SellItemState( Item item, int price, string name )
		{
			m_Item = item;
			m_Price = price;
			m_Name = name;
		}

		public Item Item => m_Item;

		public int Price => m_Price;

		public string Name => m_Name;
	}

	public class BuyItemState
	{
		private Serial m_ContSer;
		private Serial m_MySer;
		private int m_ItemID;
		private int m_Amount;
		private int m_Hue;
		private int m_Price;
		private string m_Desc;

		public BuyItemState( string name, Serial cont, Serial serial, int price, int amount, int itemID, int hue )
		{
			m_Desc = name;
			m_ContSer = cont;
			m_MySer = serial;
			m_Price = price;
			m_Amount = amount;
			m_ItemID = itemID;
			m_Hue = hue;
		}

		public int Price => m_Price;

		public Serial MySerial => m_MySer;

		public Serial ContainerSerial => m_ContSer;

		public int ItemID => m_ItemID;

		public int Amount => m_Amount;

		public int Hue => m_Hue;

		public string Description => m_Desc;
	}
}
