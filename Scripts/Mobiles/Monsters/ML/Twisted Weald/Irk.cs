using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "an Irk corpse" )]
	public class Irk : Changeling
	{
		public override string DefaultName{ get{ return "Irk"; } }
		public override int DefaultHue{ get{ return 0x489; } }

		[Constructable]
		public Irk()
		{
			IsParagon = true;

			Hue = DefaultHue;

			SetStr( 23, 183 );
			SetDex( 259, 360 );
			SetInt( 374, 600 );

			SetHits( 1006, 1064 );
			SetStam( 259, 360 );
			SetMana( 374, 600 );

			SetDamage( 14, 20 );

			SetResistance( ResistanceType.Physical, 80, 90 );
			SetResistance( ResistanceType.Fire, 40, 50 );
			SetResistance( ResistanceType.Cold, 40, 50 );
			SetResistance( ResistanceType.Poison, 41, 50 );
			SetResistance( ResistanceType.Energy, 40, 49 );

			SetSkill( SkillName.Wrestling, 120.3, 123.0 );
			SetSkill( SkillName.Tactics, 120.1, 131.8 );
			SetSkill( SkillName.MagicResist, 132.3, 165.8 );
			SetSkill( SkillName.Magery, 108.9, 119.7 );
			SetSkill( SkillName.EvalInt, 108.4, 120.0 );
			SetSkill( SkillName.Meditation, 108.9, 119.1 );

			Fame = 21000;
			Karma = -21000;
		}

		// TODO: Angry fire

		public override bool GivesMLMinorArtifact{ get{ return true; } }

		public override void GenerateLoot()
		{
			AddLoot( LootPack.UltraRich, 2 );
		}

		/*
		// TODO: uncomment once added
		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			if ( Utility.RandomDouble() < 0.25 )
				c.DropItem( new IrksBrain() );

			if ( Utility.RandomDouble() < 0.025 )
				c.DropItem( new PaladinGloves() );
		}
		*/

		public Irk( Serial serial )
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
