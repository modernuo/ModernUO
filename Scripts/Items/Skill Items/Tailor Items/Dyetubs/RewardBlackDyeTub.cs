namespace Server.Items
{
	public class RewardBlackDyeTub : DyeTub, Engines.VeteranRewards.IRewardItem
	{
		public override int LabelNumber => 1006008; // Black Dye Tub

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem { get; set; }

		[Constructible]
		public RewardBlackDyeTub()
		{
			Hue = DyedHue = 0x0001;
			Redyable = false;
			LootType = LootType.Blessed;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( IsRewardItem && !Engines.VeteranRewards.RewardSystem.CheckIsUsableBy( from, this, null ) )
				return;

			base.OnDoubleClick( from );
		}

		public RewardBlackDyeTub( Serial serial ) : base( serial )
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
		}
	}
}
