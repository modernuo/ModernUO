using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Dragon : BaseCreature
	{
		public override string CorpseName => "a dragon corpse";
		public override string DefaultName => "a dragon";

		[Constructible]
		public Dragon () : base( AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Body = Utility.RandomList( 12, 59 );
			BaseSoundID = 362;

			SetStr( 796, 825 );
			SetDex( 86, 105 );
			SetInt( 436, 475 );

			SetHits( 478, 495 );

			SetDamage( 16, 22 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 55, 65 );
			SetResistance( ResistanceType.Fire, 60, 70 );
			SetResistance( ResistanceType.Cold, 30, 40 );
			SetResistance( ResistanceType.Poison, 25, 35 );
			SetResistance( ResistanceType.Energy, 35, 45 );

			SetSkill( SkillName.EvalInt, 30.1, 40.0 );
			SetSkill( SkillName.Magery, 30.1, 40.0 );
			SetSkill( SkillName.MagicResist, 99.1, 100.0 );
			SetSkill( SkillName.Tactics, 97.6, 100.0 );
			SetSkill( SkillName.Wrestling, 90.1, 92.5 );

			Fame = 15000;
			Karma = -15000;

			VirtualArmor = 60;

			Tamable = true;
			ControlSlots = 3;
			MinTameSkill = 93.9;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.FilthyRich, 2 );
			AddLoot( LootPack.Gems, 8 );
		}

		public override bool ReacquireOnMovement => !Controlled;
		public override bool HasBreath => true; // fire breath enabled
		public override bool AutoDispel => !Controlled;
		public override int TreasureMapLevel => 4;
		public override int Meat => 19;
		public override int Hides => 20;
		public override HideType HideType => HideType.Barbed;
		public override int Scales => 7;
		public override ScaleType ScaleType => ( Body == 12 ? ScaleType.Yellow : ScaleType.Red );
		public override FoodType FavoriteFood => FoodType.Meat;
		public override bool CanAngerOnTame => true;
		public override bool CanFly => true;

		public Dragon( Serial serial ) : base( serial )
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
