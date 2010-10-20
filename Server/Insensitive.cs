/***************************************************************************
 *                               Insensitive.cs
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

namespace Server
{
	public static class Insensitive
	{
		private static IComparer m_Comparer = CaseInsensitiveComparer.Default;

		public static IComparer Comparer
		{
			get{ return m_Comparer; }
		}

		public static int Compare( string a, string b )
		{
			return m_Comparer.Compare( a, b );
		}

		public static bool Equals( string a, string b )
		{
			if ( a == null && b == null )
				return true;
			else if ( a == null || b == null || a.Length != b.Length )
				return false;

			return ( m_Comparer.Compare( a, b ) == 0 );
		}

		public static bool StartsWith( string a, string b )
		{
			if ( a == null || b == null || a.Length < b.Length )
				return false;

			return ( m_Comparer.Compare( a.Substring( 0, b.Length ), b ) == 0 );
		}

		public static bool EndsWith( string a, string b )
		{
			if ( a == null || b == null || a.Length < b.Length )
				return false;

			return ( m_Comparer.Compare( a.Substring( a.Length - b.Length ), b ) == 0 );
		}

		public static bool Contains( string a, string b )
		{
			if ( a == null || b == null || a.Length < b.Length )
				return false;

			a = a.ToLower();
			b = b.ToLower();

			return ( a.IndexOf( b ) >= 0 );
		}
	}
}