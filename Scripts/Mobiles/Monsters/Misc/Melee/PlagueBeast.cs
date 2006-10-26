using System;
using System.Collections;
using Server.Items;
using Server.Targeting;

namespace Server.Mobiles
{
	[CorpseName( "a plague beast corpse" )]
	public class PlagueBeast : BaseCreature
	{
		[Constructable]
		public PlagueBeast() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "a plague beast";
			Body = 775;

			SetStr( 302, 500 );
			SetDex( 80 );
			SetInt( 16, 20 );

			SetHits( 318, 404 );

			SetDamage( 20, 24 );

			SetDamageType( ResistanceType.Physical, 60 );
			SetDamageType( ResistanceType.Poison, 40 );

			SetResistance( ResistanceType.Physical, 45, 55 );
			SetResistance( ResistanceType.Fire, 40, 50 );
			SetResistance( ResistanceType.Cold, 25, 35 );
			SetResistance( ResistanceType.Poison, 65, 75 );
			SetResistance( ResistanceType.Energy, 25, 35 );

			SetSkill( SkillName.MagicResist, 35.0 );
			SetSkill( SkillName.Tactics, 100.0 );
			SetSkill( SkillName.Wrestling, 100.0 );

			Fame = 13000;
			Karma = -13000;

			VirtualArmor = 30;
			PackArmor( 1, 5 );
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.FilthyRich );
			AddLoot( LootPack.Gems, Utility.Random( 1, 3 ) );
			// TODO: dungeon chest, healthy gland
		}

		// TODO: Poison attack

		public override void OnDamagedBySpell( Mobile caster )
		{
			if ( this.Map != null && caster != this && 0.25 > Utility.RandomDouble() )
			{
				BaseCreature spawn = new PlagueSpawn( this );

				spawn.Team = this.Team;
				spawn.MoveToWorld( this.Location, this.Map );
				spawn.Combatant = caster;

				Say( 1053034 ); // * The plague beast creates another beast from its flesh! *
			}

			base.OnDamagedBySpell( caster );
		}

		public override bool AutoDispel{ get{ return true; } }

		public override void OnGotMeleeAttack( Mobile attacker )
		{
			if ( this.Map != null && attacker != this && 0.25 > Utility.RandomDouble() )
			{
				BaseCreature spawn = new PlagueSpawn( this );

				spawn.Team = this.Team;
				spawn.MoveToWorld( this.Location, this.Map );
				spawn.Combatant = attacker;

				Say( 1053034 ); // * The plague beast creates another beast from its flesh! *
			}

			base.OnGotMeleeAttack( attacker );
		}

		public PlagueBeast( Serial serial ) : base( serial )
		{
		}

		public override int GetIdleSound()
		{
			return 0x1BF;
		}

		public override int GetAttackSound()
		{
			return 0x1C0;
		}

		public override int GetHurtSound()
		{
			return 0x1C1;
		}

		public override int GetDeathSound()
		{
			return 0x1C2;
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