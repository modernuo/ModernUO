namespace Server.Items
{
	public class TransparentHeart : GoldEarrings
	{
		public override int LabelNumber => 1075400; // Transparent Heart

		[Constructible]
		public TransparentHeart()
		{
			LootType = LootType.Blessed;
			Hue = 0x4AB;
		}

		public TransparentHeart( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // Version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
