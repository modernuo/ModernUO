using System;
using Server.Mobiles;

namespace Server.Mobiles
{
	public class Hind : BaseCreature
	{
		public override string CorpseName => "a deer corpse";
		public override string DefaultName => "a hind";

		[Constructible]
		public Hind() : base( AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			Body = 0xED;

			SetStr( 21, 51 );
			SetDex( 47, 77 );
			SetInt( 17, 47 );

			SetHits( 15, 29 );
			SetMana( 0 );

			SetDamage( 4 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 5, 15 );
			SetResistance( ResistanceType.Cold, 5 );

			SetSkill( SkillName.MagicResist, 15.0 );
			SetSkill( SkillName.Tactics, 19.0 );
			SetSkill( SkillName.Wrestling, 26.0 );

			Fame = 300;
			Karma = 0;

			VirtualArmor = 8;

			Tamable = true;
			ControlSlots = 1;
			MinTameSkill = 23.1;
		}

		public override int Meat => 5;
		public override int Hides => 8;
		public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;

		public Hind(Serial serial) : base(serial)
		{
		}

		public override int GetAttackSound()
		{
			return 0x82;
		}

		public override int GetHurtSound()
		{
			return 0x83;
		}

		public override int GetDeathSound()
		{
			return 0x84;
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
