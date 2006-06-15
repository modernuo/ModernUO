using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
	[CorpseName( "an injured wolf corpse" )]
	public class InjuredWolf : BaseCreature
	{
		[Constructable]
		public InjuredWolf() : base( AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			Body = 0xE1;
			Name = "an injured wolf";
			BaseSoundID = 0xE5;

			Hue = Utility.RandomAnimalHue();

			SetStr( 10, 20 );
			SetDex( 45, 65 );
			SetInt( 10, 15 );

			SetHits( 1 );

			SetDamage( 1, 3 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 15 );
			SetResistance( ResistanceType.Fire, 5, 10 );

			SetSkill( SkillName.MagicResist, 10.0 );
			SetSkill( SkillName.Tactics, 0.0, 5.0 );
			SetSkill( SkillName.Wrestling, 20.0, 30.0 );
		}

		public override int GetIdleSound()
		{
			return 0xE9;
		}

		public InjuredWolf( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}