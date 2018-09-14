/***************************************************************************
 *                              StaticTarget.cs
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

namespace Server.Targeting
{
	public class StaticTarget : IPoint3D
	{
		private Point3D m_Location;
		private int m_ItemID;

		public StaticTarget( Point3D location, int itemID )
		{
			m_Location = location;
			m_ItemID = itemID & TileData.MaxItemValue;
			m_Location.Z += TileData.ItemTable[m_ItemID].CalcHeight;
		}

		[CommandProperty( AccessLevel.Counselor )]
		public Point3D Location => m_Location;

		[CommandProperty( AccessLevel.Counselor )]
		public string Name => TileData.ItemTable[m_ItemID].Name;

		[CommandProperty( AccessLevel.Counselor )]
		public TileFlag Flags => TileData.ItemTable[m_ItemID].Flags;

		[CommandProperty( AccessLevel.Counselor )]
		public int X => m_Location.X;

		[CommandProperty( AccessLevel.Counselor )]
		public int Y => m_Location.Y;

		[CommandProperty( AccessLevel.Counselor )]
		public int Z => m_Location.Z;

		[CommandProperty( AccessLevel.Counselor )]
		public int ItemID => m_ItemID;
	}
}