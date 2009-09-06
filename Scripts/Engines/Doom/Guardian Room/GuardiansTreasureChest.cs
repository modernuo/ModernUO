using System;
using Server;
using Server.Engines.Doom;

namespace Server.Items
{
	public class GuardianTreasureChest : LockableContainer
	{
		private const int m_Level = 6;

		public override bool Decays { get { return true; } }

		public override int DefaultGumpID { get { return 0x42; } }
		public override int DefaultDropSound { get { return 0x42; } }

		public override Rectangle2D Bounds
		{
			get { return new Rectangle2D( 18, 105, 144, 73 ); }
		}

		private Timer m_DecayTimer;

		[Constructable]
		public GuardianTreasureChest() : base( 0xE41 )
		{
			SetChestAppearance();
			Movable = false;

			/* TrapType = TrapType.ExplosionTrap;
			 TrapPower = m_Level * Utility.Random( 20, 35 );*/
			Locked = true;

			RequiredSkill = 99;
			LockLevel = RequiredSkill - Utility.Random( 1, 10 );
			MaxLockLevel = RequiredSkill + 21;

			// According to OSI, loot in level 4 chest is:
			//  Gold 500 - 900
			//  Reagents
			//  Scrolls
			//  Blank scrolls
			//  Potions
			//  Gems
			//  Magic Wand
			//  Magic weapon
			//  Magic armour
			//  Magic clothing (not implemented)
			//  Magic jewelry (not implemented)
			//  Crystal ball (not implemented)

			// Gold
			DropItem( new Gold( Utility.Random( 300, 325 ) ) );

			// Reagents 
			for ( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
			{
				Item ReagentLoot = Loot.RandomReagent();
				ReagentLoot.Amount = 12;
				DropItem( ReagentLoot );
			}

			// Scrolls
			for ( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
			{
				Item ScrollLoot = Loot.RandomScroll( 0, 47, SpellbookType.Regular );
				ScrollLoot.Amount = 16;
				DropItem( ScrollLoot );
			}

			// Drop blank scrolls
			DropItem( new BlankScroll( Utility.Random( 1, m_Level ) ) );

			// Potions
			for ( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
			{
				Item PotionLoot = Loot.RandomPotion();
				DropItem( PotionLoot );
			}

			// Gems
			for ( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
			{
				Item GemLoot = Loot.RandomGem();
				GemLoot.Amount = 15;
				DropItem( GemLoot );
			}

			// Magic Wand
			for ( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
				DropItem( Loot.RandomWand() );

			// Equipment
			for ( int i = Utility.Random( 1, m_Level ); i > 1; i-- )
			{
				Item item = Loot.RandomArmorOrShieldOrWeapon();

				if ( item is BaseWeapon )
				{
					BaseWeapon weapon = (BaseWeapon) item;
					weapon.DamageLevel = (WeaponDamageLevel) Utility.Random( m_Level );
					weapon.AccuracyLevel = (WeaponAccuracyLevel) Utility.Random( m_Level );
					weapon.DurabilityLevel = (WeaponDurabilityLevel) Utility.Random( m_Level );
					weapon.Quality = WeaponQuality.Regular;
				}
				else if ( item is BaseArmor )
				{
					BaseArmor armor = (BaseArmor) item;
					armor.ProtectionLevel = (ArmorProtectionLevel) Utility.Random( m_Level );
					armor.Durability = (ArmorDurabilityLevel) Utility.Random( m_Level );
					armor.Quality = ArmorQuality.Regular;
				}

				DropItem( item );
			}

			// Clothing
			for ( int i = Utility.Random( 1, 2 ); i > 1; i-- )
				DropItem( Loot.RandomClothing() );

			// Jewelry
			for ( int i = Utility.Random( 1, 2 ); i > 1; i-- )
				DropItem( Loot.RandomJewelry() );

			// Crystal ball (not implemented)
			m_DecayTimer = Timer.DelayCall( TimeSpan.FromMinutes( Utility.RandomMinMax( 1, 5 ) ), new TimerCallback( Delete ) );
		}

		private void SetChestAppearance()
		{
			bool facing = Utility.RandomBool();

			switch ( Utility.RandomList( 0, 1, 2 ) )
			{
				case 0:// Wooden Chest
					ItemID = ( facing ? 0xE42 : 0xE43 );
					GumpID = 0x49;
					break;

				case 1:// Metal Chest
					ItemID = ( facing ? 0x9AB : 0xE7C );
					GumpID = 0x4A;
					break;

				case 2:// Metal Golden Chest
					ItemID = ( facing ? 0xE40 : 0xE41 );
					GumpID = 0x42;
					break;
			}
		}

		public override void OnAfterDelete()
		{
			if ( m_DecayTimer != null || m_DecayTimer.Running )
				m_DecayTimer.Stop();

			m_DecayTimer = null;
		}

		public GuardianTreasureChest( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();

			m_DecayTimer = Timer.DelayCall( TimeSpan.FromMinutes( Utility.RandomMinMax( 1, 5 ) ), new TimerCallback( Delete ) );
		}
	}
}
