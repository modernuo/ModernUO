using System;
using Server.Mobiles;
using Server.Factions;

namespace Server.Mobiles
{
	[CorpseName("a silver serpent corpse")]
	[TypeAlias( "Server.Mobiles.Silverserpant" )]
	public class SilverSerpent : BaseCreature
	{
		public override Faction FactionAllegiance { get { return TrueBritannians.Instance; } }
		public override Ethics.Ethic EthicAllegiance { get { return Ethics.Ethic.Hero; } }

		[Constructable]
		public SilverSerpent() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Body = 92;
			Name = "a silver serpent";
			BaseSoundID = 219;

			SetStr( 161, 360 );
			SetDex( 151, 300 );
			SetInt( 21, 40 );

			SetHits( 97, 216 );

			SetDamage( 5, 21 );

			SetDamageType( ResistanceType.Physical, 50 );
			SetDamageType( ResistanceType.Poison, 50 );

			SetResistance( ResistanceType.Physical, 35, 45 );
			SetResistance( ResistanceType.Fire, 5, 10 );
			SetResistance( ResistanceType.Cold, 5, 10 );
			SetResistance( ResistanceType.Poison, 100 );
			SetResistance( ResistanceType.Energy, 5, 10 );

			SetSkill( SkillName.Poisoning, 90.1, 100.0 );
			SetSkill( SkillName.MagicResist, 95.1, 100.0 );
			SetSkill( SkillName.Tactics, 80.1, 95.0 );
			SetSkill( SkillName.Wrestling, 85.1, 100.0 );

			Fame = 7000;
			Karma = -7000;

			VirtualArmor = 40;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.Average );
			AddLoot( LootPack.Gems, 2 );
		}

		public override bool DeathAdderCharmable{ get{ return true; } }

		public override int Meat{ get{ return 1; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }
		public override Poison HitPoison{ get{ return Poison.Lethal; } }

		public SilverSerpent(Serial serial) : base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int) 0);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();

			if ( BaseSoundID == -1 )
				BaseSoundID = 219;
		}
	}
}