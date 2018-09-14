namespace Server.Mobiles
{
	public class OphidianWarrior : BaseCreature
	{
		public override string CorpseName => "an ophidian corpse";
		private static string[] m_Names = new[]
			{
				"an ophidian warrior",
				"an ophidian enforcer"
			};

		[Constructible]
		public OphidianWarrior() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = m_Names[Utility.Random( m_Names.Length )];
			Body = 86;
			BaseSoundID = 634;

			SetStr( 150, 320 );
			SetDex( 94, 190 );
			SetInt( 64, 160 );

			SetHits( 128, 155 );
			SetMana( 0 );

			SetDamage( 5, 11 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 35, 40 );
			SetResistance( ResistanceType.Fire, 20, 30 );
			SetResistance( ResistanceType.Cold, 25, 35 );
			SetResistance( ResistanceType.Poison, 30, 40 );
			SetResistance( ResistanceType.Energy, 25, 35 );

			SetSkill( SkillName.MagicResist, 70.1, 85.0 );
			SetSkill( SkillName.Swords, 60.1, 85.0 );
			SetSkill( SkillName.Tactics, 75.1, 90.0 );

			Fame = 4500;
			Karma = -4500;

			VirtualArmor = 36;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.Meager );
			AddLoot( LootPack.Average );
			AddLoot( LootPack.Gems );
		}

		public override int Meat => 1;
		public override int TreasureMapLevel => 1;

		public override OppositionGroup OppositionGroup => OppositionGroup.TerathansAndOphidians;

		public OphidianWarrior( Serial serial ) : base( serial )
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
