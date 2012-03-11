using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "a Guile corpse" )]
	public class Guile : Changeling
	{
		public override string DefaultName{ get{ return "Guile"; } }
		public override int DefaultHue{ get{ return 0x3F; } }

		[Constructable]
		public Guile()
		{
			IsParagon = true;

			Hue = DefaultHue;

			SetStr( 53, 214 );
			SetDex( 243, 367 );
			SetInt( 369, 586 );

			SetHits( 1013, 1058 );
			SetStam( 243, 367 );
			SetMana( 369, 586 );

			SetDamage( 14, 20 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 80, 90 );
			SetResistance( ResistanceType.Fire, 43, 46 );
			SetResistance( ResistanceType.Cold, 42, 44 );
			SetResistance( ResistanceType.Poison, 42, 50 );
			SetResistance( ResistanceType.Energy, 47, 50 );

			SetSkill( SkillName.Wrestling, 12.8, 16.7 );
			SetSkill( SkillName.Tactics, 102.6, 131.0 );
			SetSkill( SkillName.MagicResist, 141.2, 161.6 );
			SetSkill( SkillName.Magery, 108.4, 120.0 );
			SetSkill( SkillName.EvalInt, 108.4, 120.0 );
			SetSkill( SkillName.Meditation, 109.2, 120.0 );

			Fame = 21000;
			Karma = -21000;
		}

		public override void OnGaveMeleeAttack( Mobile defender )
		{
			base.OnGaveMeleeAttack( defender );

			if ( Utility.RandomBool() )
			{
				if ( !Kappa.IsBeingDrained( defender ) && Mana > 14 )
				{
					defender.SendLocalizedMessage( 1070848 ); // You feel your life force being stolen away.
					Kappa.BeginLifeDrain( defender, this );
					Mana -= 15;
				}
			}
		}

		public override bool GivesMLMinorArtifact{ get{ return true; } }

		public override void GenerateLoot()
		{
			AddLoot( LootPack.UltraRich, 2 );
		}

		public Guile( Serial serial )
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
