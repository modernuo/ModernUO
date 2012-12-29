using System;

namespace Server.Items
{
	public class TreasureChestLevel1 : LockableContainer
	{
		private const int m_Level = 1;

		public override bool Decays { get { return true; } }

		public override bool IsDecoContainer { get { return false; } }

		public override TimeSpan DecayTime { get { return TimeSpan.FromMinutes( Utility.Random( 15, 60 ) ); } }

		private void SetChestAppearance()
		{
			bool UseFirstItemId = Utility.RandomBool();

			switch( Utility.RandomList( 0, 1, 2 ) )
			{
				case 0:// Large Crate
					this.ItemID = ( UseFirstItemId ? 0xe3c : 0xe3d );
					this.GumpID = 0x44;
					break;

				case 1:// Medium Crate
					this.ItemID = ( UseFirstItemId ? 0xe3e : 0xe3f );
					this.GumpID = 0x44;
					break;

				case 2:// Small Crate
					this.ItemID = ( UseFirstItemId ? 0x9a9 : 0xe7e );
					this.GumpID = 0x44;
					break;
			}
		}

		public override int DefaultGumpID { get { return 0x42; } }

		public override int DefaultDropSound { get { return 0x42; } }

		public override Rectangle2D Bounds
		{
			get { return new Rectangle2D( 18, 105, 144, 73 ); }
		}

		[Constructable]
		public TreasureChestLevel1()
			: base( 0xE41 )
		{
			this.SetChestAppearance();
			Movable = false;

			TrapType = TrapType.DartTrap;
			TrapPower = m_Level * Utility.Random( 1, 25 );
			Locked = true;

			RequiredSkill = 57;
			LockLevel = this.RequiredSkill - Utility.Random( 1, 10 );
			MaxLockLevel = this.RequiredSkill + Utility.Random( 1, 10 );

			// According to OSI, loot in level 1 chest is:
			//  Gold 25 - 50
			//  Bolts 10
			//  Gems
			//  Normal weapon
			//  Normal armour
			//  Normal clothing
			//  Normal jewelry

			// Gold
			DropItem( new Gold( Utility.Random( 30, 100 ) ) );

			// Drop bolts
			//DropItem( new Bolt( 10 ) );

			// Gems
			if( Utility.RandomBool() == true )
			{
				Item GemLoot = Loot.RandomGem();
				GemLoot.Amount = Utility.Random( 1, 3 );
				DropItem( GemLoot );
			}

			// Weapon
			if( Utility.RandomBool() == true )
				DropItem( Loot.RandomWeapon() );

			// Armour
			if( Utility.RandomBool() == true )
				DropItem( Loot.RandomArmorOrShield() );

			// Clothing
			if( Utility.RandomBool() == true )
				DropItem( Loot.RandomClothing() );

			// Jewelry
			if( Utility.RandomBool() == true )
				DropItem( Loot.RandomJewelry() );
		}

		public TreasureChestLevel1( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( ( int )1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}