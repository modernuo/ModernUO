using System;

namespace Server.Items
{
	public class FurnitureDyeTub : DyeTub, Engines.VeteranRewards.IRewardItem
	{
		public override bool AllowDyables{ get{ return false; } }
		public override bool AllowFurniture{ get{ return true; } }
		public override int TargetMessage{ get{ return 501019; } } // Select the furniture to dye.
		public override int FailMessage{ get{ return 501021; } } // That is not a piece of furniture.
		public override int LabelNumber{ get{ return 1041246; } } // Furniture Dye Tub

		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get{ return m_IsRewardItem; }
			set{ m_IsRewardItem = value; }
		}

		[Constructable]
		public FurnitureDyeTub()
		{
			LootType = LootType.Blessed;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( m_IsRewardItem && !Engines.VeteranRewards.RewardSystem.CheckIsUsableBy( from, this, null ) )
				return;

			base.OnDoubleClick( from );
		}

		public FurnitureDyeTub( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( (bool) m_IsRewardItem );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_IsRewardItem = reader.ReadBool();
					break;
				}
			}

			if ( LootType == LootType.Regular )
				LootType = LootType.Blessed;
		}
	}
}