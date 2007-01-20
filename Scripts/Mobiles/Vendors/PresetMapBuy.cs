using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
	public class PresetMapBuyInfo : GenericBuyInfo
	{
		private PresetMapEntry m_Entry;

		public override bool CanCacheDisplay{ get{ return false; } }

		public PresetMapBuyInfo( PresetMapEntry entry, int price, int amount ) : base( entry.Name.ToString(), null, price, amount, 0x14EC, 0 )
		{
			m_Entry = entry;
		}

		public override IEntity GetEntity()
		{
			return new PresetMap( m_Entry );
		}
	}
}