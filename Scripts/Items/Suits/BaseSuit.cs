namespace Server.Items
{
	public abstract class BaseSuit : Item
	{
		[CommandProperty( AccessLevel.Administrator )]
		public AccessLevel AccessLevel { get; set; }

		public BaseSuit( AccessLevel level, int hue, int itemID ) : base( itemID )
		{
			Hue = hue;
			Weight = 1.0;
			Movable = false;
			LootType = LootType.Newbied;
			Layer = Layer.OuterTorso;

			AccessLevel = level;
		}

		public BaseSuit( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) AccessLevel );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					AccessLevel = (AccessLevel)reader.ReadInt();
					break;
				}
			}
		}

		public bool Validate()
		{
			object root = RootParent;

			if ( root is Mobile mobile && mobile.AccessLevel < AccessLevel )
			{
				Delete();
				return false;
			}

			return true;
		}

		public override void OnSingleClick( Mobile from )
		{
			if ( Validate() )
				base.OnSingleClick( from );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( Validate() )
				base.OnDoubleClick( from );
		}

		public override bool VerifyMove( Mobile from )
		{
			return ( from.AccessLevel >= AccessLevel );
		}

		public override bool OnEquip( Mobile from )
		{
			if ( from.AccessLevel < AccessLevel )
				from.SendMessage( "You may not wear this." );

			return ( from.AccessLevel >= AccessLevel );
		}
	}
}
