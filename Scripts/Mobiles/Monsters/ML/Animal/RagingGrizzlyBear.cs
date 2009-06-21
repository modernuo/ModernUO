using System;
using Server.Mobiles;

namespace Server.Mobiles
{
	[CorpseName( "a grizzly bear corpse" )]
	[TypeAlias( "Server.Mobiles.Grizzlybear" )]
	public class RagingGrizzlyBear : BaseCreature
	{
		[Constructable]
		public RagingGrizzlyBear() : base( AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			Name = "a raging grizzly bear";
			Body = 212;
			BaseSoundID = 0xA3;

			SetStr( 1251, 1550 );
			SetDex( 801, 1050 );
			SetInt( 151, 400 );

			SetHits( 751, 930 );
			SetMana( 0 );

			SetDamage( 6, 7 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 50, 70 );
			SetResistance( ResistanceType.Cold, 30, 50 );
			SetResistance( ResistanceType.Poison, 10, 20 );
			SetResistance( ResistanceType.Energy, 10, 20 );

			Fame = 5000;  //Guessing here
			Karma = -5000;  //Guessing here

			VirtualArmor = 24;

			Tamable = false;
			
		}

		public override int Meat{ get{ return 2; } }
		public override int Hides{ get{ return 16; } }
		public override PackInstinct PackInstinct{ get{ return PackInstinct.Bear; } }

		public RagingGrizzlyBear( Serial serial ) : base( serial )
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
