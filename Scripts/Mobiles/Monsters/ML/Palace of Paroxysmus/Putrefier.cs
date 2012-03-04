using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "a Putrefier corpse" )]
	public class Putrefier : Balron
	{
		[Constructable]
		public Putrefier()
		{
			Name = "Putrefier";
			Hue = 63;

			SetStr( 1057, 1330 );
			SetDex( 232, 450 );
			SetInt( 201, 415 );

			SetHits( 3010, 3778 );

			SetDamageType( ResistanceType.Physical, 50 );
			SetDamageType( ResistanceType.Fire, 0 );
			SetDamageType( ResistanceType.Poison, 50 );
			SetDamageType( ResistanceType.Energy, 0 );

			SetResistance( ResistanceType.Physical, 65, 80 );
			SetResistance( ResistanceType.Fire, 65, 80 );
			SetResistance( ResistanceType.Cold, 50, 60 );
			SetResistance( ResistanceType.Poison, 100 );
			SetResistance( ResistanceType.Energy, 40, 50 );

			SetSkill( SkillName.Wrestling, 111.2, 128.0 );
			SetSkill( SkillName.Tactics, 115.6, 125.2 );
			SetSkill( SkillName.MagicResist, 143.4, 163.0 );
			SetSkill( SkillName.Anatomy, 44.6, 67.0 );
			SetSkill( SkillName.Magery, 117.6, 118.8 );
			SetSkill( SkillName.EvalInt, 113.0, 128.8 );
			SetSkill( SkillName.Meditation, 41.4, 80.5 );

			PackScroll( 4, 7 );
			PackScroll( 4, 7 );
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.AosUltraRich, 4 );
		}

		/*
		// TODO: uncomment once added
		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			c.DropItem( new SpleenOfThePutrefier() );

			if ( Utility.RandomDouble() < 0.6 )
				c.DropItem( new ParrotItem() );

			if ( Paragon.ChestChance > Utility.RandomDouble() )
				c.DropItem( new ParagonChest( Name, TreasureMapLevel ) );
		}
		*/

		public override bool GivesMLMinorArtifact{ get{ return true; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }
		public override Poison HitPoison{ get{ return Poison.Lethal; } }
		public override int TreasureMapLevel{ get{ return 5; } }

		public Putrefier( Serial serial )
			: base( serial )
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
