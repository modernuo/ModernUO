using System;
using Server.Mobiles;

namespace Server.Mobiles
{
	[CorpseName( "a bull corpse" )]
	public class Bull : BaseCreature
	{
		[Constructable]
		public Bull() : base( AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			Name = "a bull";
			Body = Utility.RandomList( 0xE8, 0xE9 );
			BaseSoundID = 0x64;

			if ( 0.5 >= Utility.RandomDouble() )
				Hue = 0x901;

			SetStr( 77, 111 );
			SetDex( 56, 75 );
			SetInt( 47, 75 );

			SetHits( 50, 64 );
			SetMana( 0 );

			SetDamage( 4, 9 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 25, 30 );
			SetResistance( ResistanceType.Cold, 10, 15 );

			SetSkill( SkillName.MagicResist, 17.6, 25.0 );
			SetSkill( SkillName.Tactics, 67.6, 85.0 );
			SetSkill( SkillName.Wrestling, 40.1, 57.5 );

			Fame = 600;
			Karma = 0;

			VirtualArmor = 28;

			Tamable = true;
			ControlSlots = 1;
			MinTameSkill = 71.1;
		}

		public override int Meat{ get{ return 10; } }
		public override int Hides{ get{ return 15; } }
		public override FoodType FavoriteFood{ get{ return FoodType.GrainsAndHay; } }
		public override PackInstinct PackInstinct{ get{ return PackInstinct.Bull; } }

		public Bull(Serial serial) : base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int) 0);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();
		}
	}
}