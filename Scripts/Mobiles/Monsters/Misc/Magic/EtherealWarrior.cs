using System;
using Server;
using Server.Items;

namespace Server.Mobiles 
{ 
	[CorpseName( "an ethereal warrior corpse" )] 
	public class EtherealWarrior : BaseCreature 
	{ 
		public override bool InitialInnocent{ get{ return true; } }

		[Constructable] 
		public EtherealWarrior() : base( AIType.AI_Mage, FightMode.Evil, 10, 1, 0.2, 0.4 ) 
		{ 
			Name = NameList.RandomName( "ethereal warrior" );
			Body = 123;

			SetStr( 586, 785 );
			SetDex( 177, 255 );
			SetInt( 351, 450 );

			SetHits( 352, 471 );

			SetDamage( 13, 19 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 80, 90 );
			SetResistance( ResistanceType.Fire, 40, 50 );
			SetResistance( ResistanceType.Cold, 40, 50 );
			SetResistance( ResistanceType.Poison, 40, 50 );
			SetResistance( ResistanceType.Energy, 40, 50 );

			SetSkill( SkillName.Anatomy, 50.1, 75.0 );
			SetSkill( SkillName.EvalInt, 90.1, 100.0 );
			SetSkill( SkillName.Magery, 99.1, 100.0 );
			SetSkill( SkillName.Meditation, 90.1, 100.0 );
			SetSkill( SkillName.MagicResist, 90.1, 100.0 );
			SetSkill( SkillName.Tactics, 90.1, 100.0 );
			SetSkill( SkillName.Wrestling, 97.6, 100.0 );

			Fame = 7000;
			Karma = 7000;

			VirtualArmor = 120;
		}

		public override int TreasureMapLevel{ get{ return Core.AOS ? 5 : 0; } }

		public override void GenerateLoot()
		{
			AddLoot( LootPack.Rich, 3 );
			AddLoot( LootPack.Gems );
		}
		// TODO: Resurrect good players.
		
		public override OppositionGroup OppositionGroup
		{
			get{ return OppositionGroup.FeyAndUndead; }
		}

		public override int Feathers{ get{ return 100; } }

		public override int GetAngerSound()
		{
			return 0x2F8;
		}

		public override int GetIdleSound()
		{
			return 0x2F8;
		}

		public override int GetAttackSound()
		{
			return Utility.Random( 0x2F5, 2 );
		}

		public override int GetHurtSound()
		{
			return 0x2F9;
		}

		public override int GetDeathSound()
		{
			return 0x2F7;
		}

		public override void OnGaveMeleeAttack( Mobile defender )
		{
			base.OnGaveMeleeAttack( defender );

			defender.Damage( Utility.Random( 10, 10 ), this );
			defender.Stam -= Utility.Random( 10, 10 );
			defender.Mana -= Utility.Random( 10, 10 );
		}

		public override void OnGotMeleeAttack( Mobile attacker )
		{
			base.OnGotMeleeAttack( attacker );

			attacker.Damage( Utility.Random( 10, 10 ), this );
			attacker.Stam -= Utility.Random( 10, 10 );
			attacker.Mana -= Utility.Random( 10, 10 );
		}

		public EtherealWarrior( Serial serial ) : base( serial ) 
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