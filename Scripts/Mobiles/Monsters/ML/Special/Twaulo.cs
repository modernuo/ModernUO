using System;
using Server;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles
{
	public class Twaulo : BaseChampion
	{
		// COMPLETELY guessed beyond stats. Stats/skills come from Stratics and UOGuide. No idea what the special attacks should be.
		// Based on Silvani, due to them both using Blue creatures.

		public override ChampionSkullType SkullType{ get{ return ChampionSkullType.Pain; } }

		public override Type[] UniqueList{ get{ return new Type[] { typeof( Quell ) }; } }
		public override Type[] SharedList{ get{ return new Type[] { typeof( TheMostKnowledgePerson ), typeof( OblivionsNeedle ) }; } }
		public override Type[] DecorativeList{ get{ return new Type[] { typeof( Pier ), typeof( MonsterStatuette ) }; } }

		public override MonsterStatuetteType[] StatueTypes{ get{ return new MonsterStatuetteType[] { MonsterStatuetteType.DreadHorn }; } }

		[Constructable]
		public Twaulo()	: base(AIType.AI_Archer, FightMode.Evil)
		{
			Name = "Twaulo";
			Title = "of the Glade";
			Body = 101;
			BaseSoundID = 679;

			SetStr( 1751, 1950 );
			SetDex( 251, 450 );
			SetInt( 801, 1000 );

			SetHits( 7500 );

			SetDamage( 19, 24 );

			SetDamageType( ResistanceType.Physical, 100 );
			
			SetResistance( ResistanceType.Physical, 65, 75 );
			SetResistance( ResistanceType.Fire, 45, 55 );
			SetResistance( ResistanceType.Cold, 50, 60 );
			SetResistance( ResistanceType.Poison, 50, 60 );
			SetResistance( ResistanceType.Energy, 50, 60 );

			//Lacking OSI information skills are based on Centaurs and scaled up the way other champs are.
			SetSkill( SkillName.Anatomy, 115.1, 135.0 );
			SetSkill( SkillName.Archery, 100.0 );
			SetSkill( SkillName.MagicResist, 100.5, 150.0 );
			SetSkill( SkillName.Tactics, 100.0 );
			SetSkill( SkillName.Wrestling, 100.0 );

			Fame = 50000;
			Karma = 50000;

			VirtualArmor = 50;

			AddItem(new Bow());
			PackItem(new Arrow(Utility.RandomMinMax(500, 700)));
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.UltraRich, 2 );
		}

		public override OppositionGroup OppositionGroup
		{
			get{ return OppositionGroup.FeyAndUndead; }
		}

		public override bool Unprovokable{ get{ return true; } }
		public override Poison PoisonImmune{ get{ return Poison.Regular; } }
		public override int TreasureMapLevel{ get{ return 5; } }

		public Twaulo( Serial serial ) : base( serial )
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
