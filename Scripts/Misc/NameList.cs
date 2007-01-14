using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Server;

namespace Server
{
	public class NameList
	{
		private string m_Type;
		private string[] m_List;

		public string Type{ get{ return m_Type; } }
		public string[] List{ get{ return m_List; } }

		public bool ContainsName( string name )
		{
			for ( int i = 0; i < m_List.Length; i++ )
				if ( name == m_List[i] )
					return true;

			return false;
		}

		public NameList( string type, XmlElement xml )
		{
			m_Type = type;
			m_List = xml.InnerText.Split( ',' );

			for ( int i = 0; i < m_List.Length; ++i )
				m_List[i] = Utility.Intern( m_List[i].Trim() );
		}

		public string GetRandomName()
		{
			if ( m_List.Length > 0 )
				return m_List[Utility.Random( m_List.Length )];

			return "";
		}

		public static NameList GetNameList( string type )
		{
			NameList n = null;
			m_Table.TryGetValue( type, out n );
			return n;
		}

		public static string RandomName( string type )
		{
			NameList list = GetNameList( type );

			if ( list != null )
				return list.GetRandomName();

			return "";
		}

		private static Dictionary<string, NameList> m_Table;

		static NameList()
		{
			m_Table = new Dictionary<string, NameList>( StringComparer.OrdinalIgnoreCase );

			string filePath = Path.Combine( Core.BaseDirectory, "Data/names.xml" );

			if ( !File.Exists( filePath ) )
				return;

			try
			{
				Load( filePath );
			}
			catch ( Exception e )
			{
				Console.WriteLine( "Warning: Exception caught loading name lists:" );
				Console.WriteLine( e );
			}
		}

		private static void Load( string filePath )
		{
			XmlDocument doc = new XmlDocument();
			doc.Load( filePath );

			XmlElement root = doc["names"];

			foreach ( XmlElement element in root.GetElementsByTagName( "namelist" ) )
			{
				string type = element.GetAttribute( "type" );

				if ( String.IsNullOrEmpty( type ) )
					continue;

				try
				{
					NameList list = new NameList( type, element );

					m_Table[type] = list;
				}
				catch
				{
				}
			}
		}
	}
}
