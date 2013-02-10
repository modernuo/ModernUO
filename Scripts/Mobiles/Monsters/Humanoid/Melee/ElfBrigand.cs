using System;
using System.Collections;
using Server.Items;
using Server.ContextMenus;
using Server.Misc;
using Server.Network;

namespace Server.Mobiles
{
	// TODO: Needs some Spellweaving abilities
	public class ElfBrigand : BaseCreature
	{
		public override bool ClickTitle{ get{ return false; } }

		[Constructable]
		public ElfBrigand() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			SpeechHue = Utility.RandomDyedHue();
			Title = "the brigand";
			Race = Race.Elf;
			Hue = Race.RandomSkinHue();

			if ( this.Female = Utility.RandomBool() )
			{
				Body = 0x25E;
				Name = NameList.RandomName( "female elf brigand" );

				switch ( Utility.Random( 2 ) )
				{
					case 0: AddItem( new Skirt( Utility.RandomNondyedHue() ) ); break;
					case 1: AddItem( new Kilt( Utility.RandomNondyedHue() ) ); break;
				}
			}
			else
			{
				Body = 0x25D;
				Name = NameList.RandomName( "male elf brigand" );
				AddItem( new ShortPants( Utility.RandomNondyedHue() ) );
			}

			SetStr( 86, 100 );
			SetDex( 81, 95 );
			SetInt( 61, 75 );

			SetDamage( 10, 23 );

			SetSkill( SkillName.Fencing, 66.0, 97.5 );
			SetSkill( SkillName.Macing, 65.0, 87.5 );
			SetSkill( SkillName.MagicResist, 25.0, 47.5 );
			SetSkill( SkillName.Swords, 65.0, 87.5 );
			SetSkill( SkillName.Tactics, 65.0, 87.5 );
			SetSkill( SkillName.Wrestling, 15.0, 37.5 );

			Fame = 1000;
			Karma = -1000;

			switch ( Utility.Random( 4 ) )
			{
				case 0: AddItem( new Boots() ); break;
				case 1: AddItem( new ThighBoots() ); break;
				case 2: AddItem( new Sandals() ); break;
				case 3: AddItem( new Shoes() ); break;
			}

			AddItem( new Shirt( Utility.RandomNondyedHue() ) );

			switch ( Utility.Random( 7 ) )
			{
				case 0: AddItem( new Longsword() ); break;
				case 1: AddItem( new Cutlass() ); break;
				case 2: AddItem( new Broadsword() ); break;
				case 3: AddItem( new Axe() ); break;
				case 4: AddItem( new Club() ); break;
				case 5: AddItem( new Dagger() ); break;
				case 6: AddItem( new Spear() ); break;
			}

			Utility.AssignRandomHair( this, true );
		}

		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			if ( Utility.RandomDouble() < 0.9 )
				c.DropItem( new SeveredElfEars() );
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.Average );
		}

		public override bool AlwaysMurderer{ get{ return true; } }

		public ElfBrigand( Serial serial ) : base( serial )
		{
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
