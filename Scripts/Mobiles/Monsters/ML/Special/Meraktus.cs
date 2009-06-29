using System;
using System.Collections;
using Server.Engines.CannedEvil;
using Server.Items;
using Server.Targeting;
using Server.Misc;

namespace Server.Mobiles
{
	[CorpseName( "a minotaur corpse" )]
	public class Meraktus : BaseChampion
	{
		public override ChampionSkullType SkullType{ get{ return ChampionSkullType.Pain; } }

		public override Type[] UniqueList{ get{ return new Type[] { typeof( Subdue ) }; } }
		public override Type[] SharedList{ get{ return new Type[] { }; } }
		public override Type[] DecorativeList{ get{ return new Type[] { typeof( ArtifactLargeVase ),
										typeof( ArtifactVase ),
										typeof( MinotaurStatueDeed ) }; } }

		public override MonsterStatuetteType[] StatueTypes{ get{ return new MonsterStatuetteType[] { }; } }

		public override bool NoGoodies{ get{ return true; } }

		[Constructable]
		public Meraktus() : base( AIType.AI_Melee )
		{
			Name = "Meraktus";
			Title = "the Tormented";
			Body = 262;
		   
			SetStr( 1419, 1438 );
			SetDex( 309, 413 );
			SetInt( 129, 131 );

			SetHits( 4100, 4200 );

			SetDamage( 16, 30 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 65, 90 );
			SetResistance( ResistanceType.Fire, 65, 70 );
			SetResistance( ResistanceType.Cold, 50, 60 );
			SetResistance( ResistanceType.Poison, 40, 60 );
			SetResistance( ResistanceType.Energy, 50, 55 );

			//SetSkill( SkillName.Meditation, Unknown );
			//SetSkill( SkillName.EvalInt, Unknown );
			//SetSkill( SkillName.Magery, Unknown );
			//SetSkill( SkillName.Poisoning, Unknown );
			SetSkill( SkillName.Anatomy, 0);
			SetSkill( SkillName.MagicResist, 107.0, 111.3 );
			SetSkill( SkillName.Tactics, 107.0, 117.0 );
			SetSkill( SkillName.Wrestling, 100.0, 105.0 );

			Fame = 70000;
			Karma = -70000;

			VirtualArmor = 28; // Don't know what it should be

			// TODO: Area Ground Stomp: Damage + Dismount
		}


		public override void GenerateLoot()
		{
			AddLoot( LootPack.Rich );  // Need to verify
			AddLoot(LootPack.Rich);  // Need to verify
		}
		
		public override int GetAngerSound()
		{
			return 0x597;
		}

		public override int GetIdleSound()
		{
			return 0x596;
		}

		public override int GetAttackSound()
		{
			return 0x599;
		}

		public override int GetHurtSound()
		{
			return 0x59a;
		}

		public override int GetDeathSound()
		{
			return 0x59c;
		}

		public Meraktus( Serial serial ) : base( serial )
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
