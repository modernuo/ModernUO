using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Server;

namespace Server.Gumps
{
	public class LocationTree
	{
		private Map m_Map;
		private ParentNode m_Root;
		private Dictionary<Mobile, ParentNode> m_LastBranch;

		public LocationTree( string fileName, Map map )
		{
			m_LastBranch = new Dictionary<Mobile, ParentNode>();
			m_Map = map;

			string path = Path.Combine( "Data/Locations/", fileName );

			if ( File.Exists( path ) )
			{
				XmlTextReader xml = new XmlTextReader( new StreamReader( path ) );

				xml.WhitespaceHandling = WhitespaceHandling.None;

				m_Root = Parse( xml );

				xml.Close();
			}
		}

		public Dictionary<Mobile, ParentNode> LastBranch
		{
			get
			{
				return m_LastBranch;
			}
		}

		public Map Map
		{
			get
			{
				return m_Map;
			}
		}

		public ParentNode Root
		{
			get
			{
				return m_Root;
			}
		}

		private ParentNode Parse( XmlTextReader xml )
		{
			xml.Read();
			xml.Read();
			xml.Read();

			return new ParentNode( xml, null );
		}
	}
}