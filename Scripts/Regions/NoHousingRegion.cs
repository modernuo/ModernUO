using System;
using System.Xml;
using Server;

namespace Server.Regions
{
	public class NoHousingRegion : BaseRegion
	{
		/*  - False: this uses 'stupid OSI' house placement checking: part of the house may be placed here provided that the center is not in the region
		 *  -  True: this uses 'smart RunUO' house placement checking: no part of the house may be in the region
		 */
		private bool m_SmartChecking;

		public bool SmartChecking{ get{ return m_SmartChecking; } }

		public NoHousingRegion( XmlElement xml, Map map, Region parent ) : base( xml, map, parent )
		{
			ReadBoolean( xml["smartNoHousing"], "active", ref m_SmartChecking, false );
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return m_SmartChecking;
		}
	}
}