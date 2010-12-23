/***************************************************************************
 *                               Notoriety.cs
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
using System.Collections;
using Server;
using Server.Guilds;
using Server.Items;

namespace Server
{
	public delegate int NotorietyHandler( Mobile source, Mobile target );

	public static class Notoriety
	{
		public const int Innocent = 1;
		public const int Ally = 2;
		public const int CanBeAttacked = 3;
		public const int Criminal = 4;
		public const int Enemy = 5;
		public const int Murderer = 6;
		public const int Invulnerable = 7;

		private static NotorietyHandler m_Handler;

		public static NotorietyHandler Handler
		{
			get{ return m_Handler; }
			set{ m_Handler = value; }
		}

		private static int[] m_Hues = new int[]
			{
				0x000,
				0x059,
				0x03F,
				0x3B2,
				0x3B2,
				0x090,
				0x022,
				0x035
			};

		public static int[] Hues
		{
			get{ return m_Hues; }
			set{ m_Hues = value; }
		}

		public static int GetHue( int noto )
		{
			if ( noto < 0 || noto >= m_Hues.Length )
				return 0;

			return m_Hues[noto];
		}

		public static int Compute( Mobile source, Mobile target )
		{
			return m_Handler == null ? CanBeAttacked : m_Handler( source, target );
		}
	}
}