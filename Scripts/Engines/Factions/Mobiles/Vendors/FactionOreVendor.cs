using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Factions
{
	public class FactionOreVendor : BaseFactionVendor
	{
		public FactionOreVendor( Town town, Faction faction ) : base( town, faction, "the Ore Man" )
		{
			// NOTE: Skills verified
			SetSkill( SkillName.Carpentry, 85.0, 100.0 );
			SetSkill( SkillName.Lumberjacking, 60.0, 83.0 );
		}

		public override void InitSBInfo()
		{
			SBInfos.Add( new SBFactionOre() );
		}

		public override void InitOutfit()
		{
			base.InitOutfit();

			AddItem( new HalfApron() );
		}

		public FactionOreVendor( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class SBFactionOre : SBInfo
	{
		private ArrayList m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBFactionOre()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override ArrayList BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : ArrayList
		{
			public InternalBuyInfo()
			{
				for ( int i = 0; i < 5; ++i )
					Add( new GenericBuyInfo( typeof( IronOre ), 16, 20, 0x19B8, 0 ) );
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
			}
		}
	}
}