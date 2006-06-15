using System;
using Server;
using Server.Ethics;
using Server.Mobiles;

namespace Server.Mobiles
{
	[CorpseName( "an evil corpse" )]
	public class UnholyFamiliar : BaseCreature
	{
		public override bool IsDispellable { get { return false; } }
		public override bool IsBondable { get { return false; } }

		[Constructable]
		public UnholyFamiliar()
			: base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "a dark wolf";
			Body = 99;
			BaseSoundID = 0xE5;

			SetStr( 96, 120 );
			SetDex( 81, 105 );
			SetInt( 36, 60 );

			SetHits( 58, 72 );
			SetMana( 0 );

			SetDamage( 11, 17 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 20, 25 );
			SetResistance( ResistanceType.Fire, 10, 20 );
			SetResistance( ResistanceType.Cold, 5, 10 );
			SetResistance( ResistanceType.Poison, 5, 10 );
			SetResistance( ResistanceType.Energy, 10, 15 );

			SetSkill( SkillName.MagicResist, 57.6, 75.0 );
			SetSkill( SkillName.Tactics, 50.1, 70.0 );
			SetSkill( SkillName.Wrestling, 60.1, 80.0 );

			Fame = 2500;
			Karma = 2500;

			VirtualArmor = 22;

			Tamable = false;
			ControlSlots = 1;
		}

		public override int Meat { get { return 1; } }
		public override int Hides { get { return 7; } }
		public override FoodType FavoriteFood { get { return FoodType.Meat; } }
		public override PackInstinct PackInstinct { get { return PackInstinct.Canine; } }

		public UnholyFamiliar( Serial serial )
			: base( serial )
		{
		}

		public override string ApplyNameSuffix( string suffix )
		{
			if ( suffix.Length == 0 )
				suffix = Ethic.Evil.Definition.Adjunct.String;
			else
				suffix = String.Concat( suffix, " ", Ethic.Evil.Definition.Adjunct.String );

			return base.ApplyNameSuffix( suffix );
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