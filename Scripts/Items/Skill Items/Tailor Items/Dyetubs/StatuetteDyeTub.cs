namespace Server.Items
{
	public class StatuetteDyeTub : DyeTub, Engines.VeteranRewards.IRewardItem
	{
		public override bool AllowDyables => false;
		public override bool AllowStatuettes => true;
		public override int TargetMessage => 1049777; // Target the statuette to dye
		public override int FailMessage => 1049778; // You can only dye veteran reward statuettes with this tub.
		public override int LabelNumber => 1049741; // Reward Statuette Dye Tub
		public override CustomHuePicker CustomHuePicker => CustomHuePicker.LeatherDyeTub;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem { get; set; }

		[Constructible]
		public StatuetteDyeTub()
		{
			LootType = LootType.Blessed;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( IsRewardItem && !Engines.VeteranRewards.RewardSystem.CheckIsUsableBy( from, this, null ) )
				return;

			base.OnDoubleClick( from );
		}

		public StatuetteDyeTub( Serial serial ) : base( serial )
		{
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( Core.ML && IsRewardItem )
				list.Add( 1076221 ); // 5th Year Veteran Reward
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
