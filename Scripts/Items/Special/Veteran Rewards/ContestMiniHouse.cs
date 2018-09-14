using Server.Engines.VeteranRewards;

namespace Server.Items
{
	public class ContestMiniHouse : MiniHouseAddon
	{				
		public override BaseAddonDeed Deed
		{ 
			get
			{ 
				ContestMiniHouseDeed deed = new ContestMiniHouseDeed( Type );
				deed.IsRewardItem = m_IsRewardItem;

				return deed; 
			} 
		}

		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get => m_IsRewardItem;
			set{ m_IsRewardItem = value; InvalidateProperties(); }
		}

		[Constructible]
		public ContestMiniHouse() : base( MiniHouseType.MalasMountainPass )
		{
		}

		[Constructible]
		public ContestMiniHouse( MiniHouseType type ) : base( type )
		{
		}

		public ContestMiniHouse( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version

			writer.Write( (bool) m_IsRewardItem );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
			
			m_IsRewardItem = reader.ReadBool();
		}
	}

	public class ContestMiniHouseDeed : MiniHouseDeed, IRewardItem
	{
		public override BaseAddon Addon
		{ 
			get
			{ 
				ContestMiniHouse addon = new ContestMiniHouse( Type );
				addon.IsRewardItem = m_IsRewardItem;

				return addon; 
			} 
		}
		
		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get => m_IsRewardItem;
			set{ m_IsRewardItem = value; InvalidateProperties(); }
		}	
		
		[Constructible]
		public ContestMiniHouseDeed() : base( MiniHouseType.MalasMountainPass )
		{
		}

		[Constructible]
		public ContestMiniHouseDeed( MiniHouseType type ) : base( type )
		{
		}

		public ContestMiniHouseDeed( Serial serial ) : base( serial )
		{
		}
		
		public override void OnDoubleClick( Mobile from )
		{
			if ( m_IsRewardItem && !RewardSystem.CheckIsUsableBy( from, this, new object[] { Type } ) )
				return;

			base.OnDoubleClick( from );
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( Core.ML && m_IsRewardItem )
				list.Add( 1076217 ); // 1st Year Veteran Reward
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version

			writer.Write( (bool) m_IsRewardItem );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
			
			m_IsRewardItem = reader.ReadBool();
		}
	}
}
