namespace Server.Items
{
	public class BasinOfCrystalClearWater : Item
	{
		public override int LabelNumber => 1075303; // Basin of Crystal-Clear Water

		public override bool Nontransferable => true;

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructible]
		public BasinOfCrystalClearWater() : base( 0x1008 )
		{
			LootType = LootType.Blessed;
		}

		public BasinOfCrystalClearWater( Serial serial ) : base( serial )
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
