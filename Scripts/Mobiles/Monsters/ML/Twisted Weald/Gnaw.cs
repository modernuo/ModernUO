using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "a Gnaw corpse" )]
	public class Gnaw : DireWolf
	{
		[Constructable]
		public Gnaw()
		{
			IsParagon = true;

			Name = "Gnaw";
			Hue = 0x130;

			SetStr( 151, 172 );
			SetDex( 124, 145 );
			SetInt( 60, 86 );

			SetHits( 817, 857 );
			SetStam( 124, 145 );
			SetMana( 52, 86 );

			SetDamage( 16, 22 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 64, 69 );
			SetResistance( ResistanceType.Fire, 53, 56 );
			SetResistance( ResistanceType.Cold, 22, 27 );
			SetResistance( ResistanceType.Poison, 27, 30 );
			SetResistance( ResistanceType.Energy, 21, 34 );

			SetSkill( SkillName.Wrestling, 106.4, 116.5 );
			SetSkill( SkillName.Tactics, 84.1, 103.2 );
			SetSkill( SkillName.MagicResist, 96.8, 110.7 );

			Fame = 17500;
			Karma = -17500;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.FilthyRich, 2 );
		}

		public override bool GivesMLMinorArtifact{ get{ return true; } }
		public override int Hides{ get{ return 28; } }
		public override int Meat{ get{ return 4; } }

		/*
		// TODO: uncomment once added
		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			if ( Utility.RandomDouble() < 0.3 )
				c.DropItem( new GnawsFang() );
		}
		*/

		public Gnaw( Serial serial )
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
