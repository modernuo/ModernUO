using System;
using System.Collections;
using Server.Items;
using Server.Targeting;

namespace Server.Mobiles
{
	public class Ogre : BaseCreature
	{
		public override string CorpseName => "an ogre corpse";
		public override string DefaultName => "an ogre";

		[Constructible]
		public Ogre () : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Body = 1;
			BaseSoundID = 427;

			SetStr( 166, 195 );
			SetDex( 46, 65 );
			SetInt( 46, 70 );

			SetHits( 100, 117 );
			SetMana( 0 );

			SetDamage( 9, 11 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 30, 35 );
			SetResistance( ResistanceType.Fire, 15, 25 );
			SetResistance( ResistanceType.Cold, 15, 25 );
			SetResistance( ResistanceType.Poison, 15, 25 );
			SetResistance( ResistanceType.Energy, 25 );

			SetSkill( SkillName.MagicResist, 55.1, 70.0 );
			SetSkill( SkillName.Tactics, 60.1, 70.0 );
			SetSkill( SkillName.Wrestling, 70.1, 80.0 );

			Fame = 3000;
			Karma = -3000;

			VirtualArmor = 32;

			PackItem( new Club() );
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.Average );
			AddLoot( LootPack.Potions );
		}

		public override bool CanRummageCorpses => true;
		public override int TreasureMapLevel => 1;
		public override int Meat => 2;

		public Ogre( Serial serial ) : base( serial )
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
