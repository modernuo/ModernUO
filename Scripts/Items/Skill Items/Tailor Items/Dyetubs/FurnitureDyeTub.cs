namespace Server.Items
{
	public class FurnitureDyeTub : DyeTub, Engines.VeteranRewards.IRewardItem
	{
		public override bool AllowDyables => false;
		public override bool AllowFurniture => true;
		public override int TargetMessage => 501019; // Select the furniture to dye.
		public override int FailMessage => 501021; // That is not a piece of furniture.
		public override int LabelNumber => 1041246; // Furniture Dye Tub

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem { get; set; }

		[Constructible]
		public FurnitureDyeTub()
		{
			LootType = LootType.Blessed;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( IsRewardItem && !Engines.VeteranRewards.RewardSystem.CheckIsUsableBy( from, this, null ) )
				return;

			base.OnDoubleClick( from );
		}

		public FurnitureDyeTub( Serial serial ) : base( serial )
		{
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( Core.ML && IsRewardItem )
				list.Add( 1076217 ); // 1st Year Veteran Reward
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( (bool) IsRewardItem );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					IsRewardItem = reader.ReadBool();
					break;
				}
			}

			if ( LootType == LootType.Regular )
				LootType = LootType.Blessed;
		}
	}
}
