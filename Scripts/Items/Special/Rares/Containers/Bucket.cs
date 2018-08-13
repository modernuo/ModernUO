namespace Server.Items
{
	class Bucket : BaseWaterContainer
	{
		public override int voidItem_ID => vItemID;
		public override int fullItem_ID => fItemID;
		public override int MaxQuantity => 25;

		private static int vItemID = 0x14e0;
		private static int fItemID = 0x2004;

		[Constructible]
		public Bucket()
			: this( false )
		{
		}

		[Constructible]
		public Bucket( bool filled )
			: base( ( filled ) ? Bucket.fItemID : Bucket.vItemID, filled )
		{
		}

		public Bucket( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
