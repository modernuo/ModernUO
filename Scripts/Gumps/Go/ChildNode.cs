using System;
using System.Xml;
using Server;

namespace Server.Gumps
{
	public class ChildNode
	{
		private ParentNode m_Parent;

		private string m_Name;
		private Point3D m_Location;

		public ChildNode( XmlTextReader xml, ParentNode parent )
		{
			m_Parent = parent;

			Parse( xml );
		}

		private void Parse( XmlTextReader xml )
		{
			if ( xml.MoveToAttribute( "name" ) )
				m_Name = xml.Value;
			else
				m_Name = "empty";

			int x = 0, y = 0, z = 0;

			if ( xml.MoveToAttribute( "x" ) )
				x = Utility.ToInt32( xml.Value );

			if ( xml.MoveToAttribute( "y" ) )
				y = Utility.ToInt32( xml.Value );

			if ( xml.MoveToAttribute( "z" ) )
				z = Utility.ToInt32( xml.Value );

			m_Location = new Point3D( x, y, z );
		}

		public ParentNode Parent
		{
			get
			{
				return m_Parent;
			}
		}

		public string Name
		{
			get
			{
				return m_Name;
			}
		}

		public Point3D Location
		{
			get
			{
				return m_Location;
			}
		}
	}
}