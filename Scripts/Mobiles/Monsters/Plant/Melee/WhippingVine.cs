using System;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "a whipping vine corpse" )]
	public class WhippingVine : BaseCreature
	{
		[Constructable]
		public WhippingVine() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "a whipping vine";
			Body = 8;
			Hue = 0x851;
			BaseSoundID = 352;

			SetStr( 251, 300 );
			SetDex( 76, 100 );
			SetInt( 26, 40 );

			SetMana( 0 );

			SetDamage( 7, 25 );

			SetDamageType( ResistanceType.Physical, 70 );
			SetDamageType( ResistanceType.Poison, 30 );

			SetResistance( ResistanceType.Physical, 75, 85 );
			SetResistance( ResistanceType.Fire, 15, 25 );
			SetResistance( ResistanceType.Cold, 15, 25 );
			SetResistance( ResistanceType.Poison, 75, 85 );
			SetResistance( ResistanceType.Energy, 35, 45 );

			SetSkill( SkillName.MagicResist, 70.0 );
			SetSkill( SkillName.Tactics, 70.0 );
			SetSkill( SkillName.Wrestling, 70.0 );

			Fame = 1000;
			Karma = -1000;

			VirtualArmor = 45;

			PackReg( 3 );
			PackItem( new FertileDirt( Utility.RandomMinMax( 1, 10 ) ) );

			if ( 0.2 >= Utility.RandomDouble() )
				PackItem( new ExecutionersCap() );

			PackItem( new Vines() );
		}

		public override bool BardImmune{ get{ return !Core.AOS; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }

		public WhippingVine( Serial serial ) : base( serial )
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