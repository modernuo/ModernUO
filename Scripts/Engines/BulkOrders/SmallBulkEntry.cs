using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Server;

namespace Server.Engines.BulkOrders
{
	public class SmallBulkEntry
	{
		private Type m_Type;
		private int m_Number;
		private int m_Graphic;

		public Type Type{ get{ return m_Type; } }
		public int Number{ get{ return m_Number; } }
		public int Graphic{ get{ return m_Graphic; } }

		public SmallBulkEntry( Type type, int number, int graphic )
		{
			m_Type = type;
			m_Number = number;
			m_Graphic = graphic;
		}

		public static SmallBulkEntry[] BlacksmithWeapons
		{
			get{ return GetEntries( "Blacksmith", "weapons" ); }
		}

		public static SmallBulkEntry[] BlacksmithArmor
		{
			get{ return GetEntries( "Blacksmith", "armor" ); }
		}

		public static SmallBulkEntry[] TailorCloth
		{
			get{ return GetEntries( "Tailoring", "cloth" ); }
		}

		public static SmallBulkEntry[] TailorLeather
		{
			get{ return GetEntries( "Tailoring", "leather" ); }
		}

		private static Hashtable m_Cache;

		public static SmallBulkEntry[] GetEntries( string type, string name )
		{
			if ( m_Cache == null )
				m_Cache = new Hashtable();

			Hashtable table = (Hashtable)m_Cache[type];

			if ( table == null )
				m_Cache[type] = table = new Hashtable();

			SmallBulkEntry[] entries = (SmallBulkEntry[])table[name];

			if ( entries == null )
				table[name] = entries = LoadEntries( type, name );

			return entries;
		}

		public static SmallBulkEntry[] LoadEntries( string type, string name )
		{
			return LoadEntries( String.Format( "Data/Bulk Orders/{0}/{1}.cfg", type, name ) );
		}

		public static SmallBulkEntry[] LoadEntries( string path )
		{
			path = Path.Combine( Core.BaseDirectory, path );

			List<SmallBulkEntry> list = new List<SmallBulkEntry>();

			if ( File.Exists( path ) )
			{
				using ( StreamReader ip = new StreamReader( path ) )
				{
					string line;

					while ( (line = ip.ReadLine()) != null )
					{
						if ( line.Length == 0 || line.StartsWith( "#" ) )
							continue;

						try
						{
							string[] split = line.Split( '\t' );

							if ( split.Length >= 2 )
							{
								Type type = ScriptCompiler.FindTypeByName( split[0] );
								int graphic = Utility.ToInt32( split[split.Length - 1] );

								if ( type != null && graphic > 0 )
									list.Add( new SmallBulkEntry( type, 1020000 + graphic, graphic ) );
							}
						}
						catch
						{
						}
					}
				}
			}

			return list.ToArray();
		}
	}
}