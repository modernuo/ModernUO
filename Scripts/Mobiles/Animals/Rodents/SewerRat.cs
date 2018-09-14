namespace Server.Mobiles
{
	[TypeAlias("Server.Mobiles.Sewerrat")]
	public class SewerRat : BaseCreature
	{
		public override string CorpseName => "a rat corpse";
		public override string DefaultName => "a sewer rat";

		[Constructible]
		public SewerRat() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Body = 238;
			BaseSoundID = 0xCC;

			SetStr( 9 );
			SetDex( 25 );
			SetInt( 6, 10 );

			SetHits( 6 );
			SetMana( 0 );

			SetDamage( 1, 2 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 5, 10 );
			SetResistance( ResistanceType.Poison, 15, 25 );
			SetResistance( ResistanceType.Energy, 5, 10 );

			SetSkill( SkillName.MagicResist, 5.0 );
			SetSkill( SkillName.Tactics, 5.0 );
			SetSkill( SkillName.Wrestling, 5.0 );

			Fame = 300;
			Karma = -300;

			VirtualArmor = 6;

			Tamable = true;
			ControlSlots = 1;
			MinTameSkill = -0.9;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.Poor );
		}

		public override int Meat => 1;
		public override FoodType FavoriteFood => FoodType.Meat | FoodType.Eggs | FoodType.FruitsAndVegies;

		public SewerRat(Serial serial) : base(serial)
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
