using System;

namespace Server.Items
{
	public class MetallicLeatherDyeTub : LeatherDyeTub
	{
		public override CustomHuePicker CustomHuePicker{ get{ return null; } }

		public override int LabelNumber { get { return 1153495; } } // Metallic Leather ... 

		public override bool MetallicHues { get { return true; } }

		[Constructable]
		public MetallicLeatherDyeTub()
		{
			LootType = LootType.Blessed;
		}

		public MetallicLeatherDyeTub( Serial serial )
			: base( serial )
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

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}