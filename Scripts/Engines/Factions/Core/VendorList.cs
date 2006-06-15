using System;
using Server;

namespace Server.Factions
{
	public class VendorList
	{
		private VendorDefinition m_Definition;
		private FactionVendorCollection m_Vendors;

		public VendorDefinition Definition{ get{ return m_Definition; } }
		public FactionVendorCollection Vendors{ get{ return m_Vendors; } }

		public BaseFactionVendor Construct( Town town, Faction faction )
		{
			try{ return Activator.CreateInstance( m_Definition.Type, new object[]{ town, faction } ) as BaseFactionVendor; }
			catch{ return null; }
		}

		public VendorList( VendorDefinition definition )
		{
			m_Definition = definition;
			m_Vendors = new FactionVendorCollection();
		}
	}
}