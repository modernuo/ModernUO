/***************************************************************************
 *                                 Prompt.cs
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

namespace Server.Prompts
{
	public abstract class Prompt
	{
		private int m_Serial;
		private static int m_Serials;

		public int Serial
		{
			get
			{
				return m_Serial;
			}
		}

		protected Prompt()
		{
			do
			{
				m_Serial = ++m_Serials;
			} while ( m_Serial == 0 );
		}

		public virtual void OnCancel( Mobile from )
		{
		}

		public virtual void OnResponse( Mobile from, string text )
		{
		}
	}
}