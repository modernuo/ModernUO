using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "a Miasma corpse" )]
	public class Miasma : Scorpion
	{
		[Constructable]
		public Miasma()
		{
			IsParagon = true;

			Name = "Miasma";
			Hue = 0x8FD;

			SetStr( 255, 847 );
			SetDex( 145, 428 );
			SetInt( 26, 380 );

			SetHits( 750, 2000 );
			SetMana( 5, 60 );

			SetDamage( 20, 30 );

			SetDamageType( ResistanceType.Physical, 60 );
			SetDamageType( ResistanceType.Poison, 40 );

			SetResistance( ResistanceType.Physical, 50, 54 );
			SetResistance( ResistanceType.Fire, 40, 45 );
			SetResistance( ResistanceType.Cold, 50, 55 );
			SetResistance( ResistanceType.Poison, 70, 80 );
			SetResistance( ResistanceType.Energy, 40, 45 );

			SetSkill( SkillName.Wrestling, 84.9, 103.3 );
			SetSkill( SkillName.Tactics, 98.4, 110.6 );
			SetSkill( SkillName.Anatomy, 0 );
			SetSkill( SkillName.MagicResist, 74.4, 77.7 );
			SetSkill( SkillName.Poisoning, 128.5, 143.6 );

			Fame = 21000;
			Karma = -21000;
		}

			/* yes, this is OSI style */
		public override double WeaponAbilityChance { get { return 0.75; } }
		public override double HitPoisonChance { get { return 0.35; } }
		public override Poison HitPoison { get { return ( Poison.Lethal ); } }
		public override bool HasManaOveride { get { return true; } }
		public override bool GivesMLMinorArtifact { get { return true; } }
		public override int TreasureMapLevel { get { return 5; } }

		public override void GenerateLoot()
		{
			AddLoot( LootPack.UltraRich, 4 );
		}

		public override WeaponAbility GetWeaponAbility()
		{
			return WeaponAbility.MortalStrike;
		}

		/*
		// TODO: uncomment once added
		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			if ( Utility.RandomDouble() < 0.025 )
			{
				switch ( Utility.Random( 16 ) )
				{
					case 0: c.DropItem( new MyrmidonGloves() ); break;
					case 1: c.DropItem( new MyrmidonGorget() ); break;
					case 2: c.DropItem( new MyrmidonLegs() ); break;
					case 3: c.DropItem( new MyrmidonArms() ); break;
					case 4: c.DropItem( new PaladinArms() ); break;
					case 5: c.DropItem( new PaladinGorget() ); break;
					case 6: c.DropItem( new LeafweaveLegs() ); break;
					case 7: c.DropItem( new DeathChest() ); break;
					case 8: c.DropItem( new DeathGloves() ); break;
					case 9: c.DropItem( new DeathLegs() ); break;
					case 10: c.DropItem( new GreymistGloves() ); break;
					case 11: c.DropItem( new GreymistArms() ); break;
					case 12: c.DropItem( new AssassinChest() ); break;
					case 13: c.DropItem( new AssassinArms() ); break;
					case 14: c.DropItem( new HunterGloves() ); break;
					case 15: c.DropItem( new HunterLegs() ); break;
				}
			}
		}
		*/

		public Miasma( Serial serial )
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
