using System;
using Server;
using Server.Mobiles;

namespace Server.Mobiles
{
	[CorpseName( "a war horse corpse" )]
	public abstract class BaseWarHorse : BaseMount
	{
		public BaseWarHorse( int bodyID, int itemID, AIType aiType, FightMode fightMode, int rangePerception, int rangeFight, double activeSpeed, double passiveSpeed ) : base ( "a war horse", bodyID, itemID, aiType, fightMode, rangePerception, rangeFight, activeSpeed, passiveSpeed )
		{
			BaseSoundID = 0xA8;

			InitStats( Utility.Random( 300, 100 ), 125, 60 );

			SetStr( 400 );
			SetDex( 125 );
			SetInt( 51, 55 );

			SetHits( 240 );
			SetMana( 0 );

			SetDamage( 5, 8 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 40, 50 );
			SetResistance( ResistanceType.Fire, 30, 40 );
			SetResistance( ResistanceType.Cold, 30, 40 );
			SetResistance( ResistanceType.Poison, 30, 40 );
			SetResistance( ResistanceType.Energy, 30, 40 );

			SetSkill( SkillName.MagicResist, 25.1, 30.0 );
			SetSkill( SkillName.Tactics, 29.3, 44.0 );
			SetSkill( SkillName.Wrestling, 29.3, 44.0 );

			Fame = 300;
			Karma = 300;

			Tamable = true;
			ControlSlots = 1;
			MinTameSkill = 29.1;
		}

		public override FoodType FavoriteFood{ get{ return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

		public BaseWarHorse( Serial serial ) : base( serial )
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