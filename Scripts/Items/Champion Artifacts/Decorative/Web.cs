namespace Server.Items
{
	public class Web : Item
	{
		private static int[] m_itemids = new[]
		{
			0x10d7, 0x10d8, 0x10dd
		};

		[Constructible]
		public Web()
			: base(m_itemids[Utility.Random(3)])
		{
		}

		[Constructible]
		public Web( int itemid ) : base( itemid )
		{
		}

		public Web( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}
