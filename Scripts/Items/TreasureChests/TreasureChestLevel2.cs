using System;

namespace Server.Items
{
	public class TreasureChestLevel2 : LockableContainer
	{
		private const int m_Level = 2;

		public override bool Decays { get { return true; } }

		public override bool IsDecoContainer { get { return false; } }

		public override TimeSpan DecayTime { get { return TimeSpan.FromMinutes( Utility.Random( 15, 60 ) ); } }

		private void SetChestAppearance()
		{
			bool UseFirstItemId = Utility.RandomBool();

			switch( Utility.RandomList( 0, 1, 2, 3, 4, 5, 6, 7 ) )
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

				case 3:// Wooden Chest
					this.ItemID = ( UseFirstItemId ? 0xe42 : 0xe43 );
					this.GumpID = 0x49;
					break;

				case 4:// Metal Chest
					this.ItemID = ( UseFirstItemId ? 0x9ab : 0xe7c );
					this.GumpID = 0x4A;
					break;

				case 5:// Metal Golden Chest
					this.ItemID = ( UseFirstItemId ? 0xe40 : 0xe41 );
					this.GumpID = 0x42;
					break;

				case 6:// Keg
					this.ItemID = ( UseFirstItemId ? 0xe7f : 0xe7f );
					this.GumpID = 0x3e;
					break;

				case 7:// Barrel
					this.ItemID = ( UseFirstItemId ? 0xe77 : 0xe77 );
					this.GumpID = 0x3e;
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
		public TreasureChestLevel2()
			: base( 0xE41 )
		{
			this.SetChestAppearance();
			Movable = false;

			TrapType = TrapType.ExplosionTrap;
			TrapPower = m_Level * Utility.Random( 1, 25 );
			Locked = true;

			RequiredSkill = 72;
			LockLevel = this.RequiredSkill - Utility.Random( 1, 10 );
			MaxLockLevel = this.RequiredSkill + Utility.Random( 1, 10 ); ;

			// According to OSI, loot in level 2 chest is:
			//  Gold 80 - 150
			//  Arrows 10
			//  Reagents
			//  Scrolls
			//  Potions
			//  Gems

			// Gold
			DropItem( new Gold( Utility.Random( 70, 100 ) ) );

			// Drop bolts
			//DropItem( new Arrow( 10 ) );

			// Reagents
			for( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
			{
				Item ReagentLoot = Loot.RandomReagent();
				ReagentLoot.Amount = Utility.Random( 1, m_Level );
				DropItem( ReagentLoot );
			}

			// Scrolls
			for( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
			{
				Item ScrollLoot = Loot.RandomScroll( 0, 39, SpellbookType.Regular );
				ScrollLoot.Amount = Utility.Random( 1, 8 );
				DropItem( ScrollLoot );
			}

			// Potions
			for( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
			{
				Item PotionLoot = Loot.RandomPotion();
				DropItem( PotionLoot );
			}

			// Gems
			for( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
			{
				Item GemLoot = Loot.RandomGem();
				GemLoot.Amount = Utility.Random( 1, 6 );
				DropItem( GemLoot );
			}
		}

		public TreasureChestLevel2( Serial serial )
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