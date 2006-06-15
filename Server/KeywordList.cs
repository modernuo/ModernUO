/***************************************************************************
 *                               KeywordList.cs
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

namespace Server
{
	public class KeywordList
	{
		private int[] m_Keywords;
		private int m_Count;

		public KeywordList()
		{
			m_Keywords = new int[8];
			m_Count = 0;
		}

		public int Count
		{
			get
			{
				return m_Count;
			}
		}

		public bool Contains( int keyword )
		{
			bool contains = false;

			for ( int i = 0; !contains && i < m_Count; ++i )
				contains = ( keyword == m_Keywords[i] );

			return contains;
		}

		public void Add( int keyword )
		{
			if ( (m_Count + 1) > m_Keywords.Length )
			{
				int[] old = m_Keywords;
				m_Keywords = new int[old.Length * 2];

				for ( int i = 0; i < old.Length; ++i )
					m_Keywords[i] = old[i];
			}

			m_Keywords[m_Count++] = keyword;
		}

		private static int[] m_EmptyInts = new int[0];

		public int[] ToArray()
		{
			if ( m_Count == 0 )
				return m_EmptyInts;

			int[] keywords = new int[m_Count];

			for ( int i = 0; i < m_Count; ++i )
				keywords[i] = m_Keywords[i];

			m_Count = 0;

			return keywords;
		}
	}
}