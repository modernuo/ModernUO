using System;
using System.Collections;
using System.IO;
using System.Net;

namespace Server
{
	public class Firewall
	{
		private static ArrayList m_Blocked;

		static Firewall()
		{
			m_Blocked = new ArrayList();

			string path = "firewall.cfg";

			if ( File.Exists( path ) )
			{
				using ( StreamReader ip = new StreamReader( path ) )
				{
					string line;

					while ( (line = ip.ReadLine()) != null )
					{
						line = line.Trim();

						if ( line.Length == 0 )
							continue;

						object toAdd;

						IPAddress addr;
						if( IPAddress.TryParse( line, out addr ) )
							toAdd = addr;
						else
							toAdd = line;

						m_Blocked.Add( toAdd.ToString() );
					}
				}
			}
		}

		public static ArrayList List
		{
			get
			{
				return m_Blocked;
			}
		}

		public static void RemoveAt( int index )
		{
			m_Blocked.RemoveAt( index );
			Save();
		}

		public static void Remove( string pattern )
		{
			m_Blocked.Remove( pattern );
			Save();
		}

		public static void Remove( IPAddress ip )
		{
			m_Blocked.Remove( ip );
			Save();
		}

		public static void Add( object obj )
		{
			if ( !(obj is IPAddress) && !(obj is String) )
				return;

			if ( !m_Blocked.Contains( obj ) )
				m_Blocked.Add( obj );

			Save();
		}

		public static void Add( string pattern )
		{
			if ( !m_Blocked.Contains( pattern ) )
				m_Blocked.Add( pattern );

			Save();
		}

		public static void Add( IPAddress ip )
		{
			if ( !m_Blocked.Contains( ip ) )
				m_Blocked.Add( ip );

			Save();
		}

		public static void Save()
		{
			string path = "firewall.cfg";

			using ( StreamWriter op = new StreamWriter( path ) )
			{
				for ( int i = 0; i < m_Blocked.Count; ++i )
					op.WriteLine( m_Blocked[i] );
			}
		}

		public static bool IsBlocked( IPAddress ip )
		{
			bool contains = false;

			for ( int i = 0; !contains && i < m_Blocked.Count; ++i )
			{
				if ( m_Blocked[i] is IPAddress )
					contains = ip.Equals( m_Blocked[i] );
                else if ( m_Blocked[i] is String )
                {
                    string s = (string)m_Blocked[i];

                    contains = Utility.IPMatchCIDR( s, ip );

                    if( !contains )
                        contains = Utility.IPMatch( s, ip );
                }
			}

			return contains;
		}
	}
}