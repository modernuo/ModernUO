using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Twaulo : BaseCreature
	{
		// COMPLETELY guessed beyond stats. Stats/skills come from Stratics and UOGuide. No idea what the special attacks should be.
		// Based on Silvani, due to them both using Blue creatures.
		[Constructable]
		public Twaulo()
			: base(AIType.AI_Archer, FightMode.Evil, 18, 1, 0.1, 0.2)
		{
			Name = "Twaulo";
			Title = "of the Glade";
			Body = 101;
			BaseSoundID = 679;

			SetStr( 1751, 1950 );
			SetDex( 251, 450 );
			SetInt( 801, 1000 );

			SetHits( 7500 );

			SetDamage( 3, 4 );

			SetDamageType( ResistanceType.Physical, 100 );
			
			SetResistance( ResistanceType.Physical, 65, 75 );
			SetResistance( ResistanceType.Fire, 45, 55 );
			SetResistance( ResistanceType.Cold, 50, 60 );
			SetResistance( ResistanceType.Poison, 50, 60 );
			SetResistance( ResistanceType.Energy, 50, 60 );

			SetSkill( SkillName.Archery, 85.0, 100 ); // Guestimate
			SetSkill( SkillName.EvalInt, 0 ); // Per Stratics?!?
			SetSkill( SkillName.Magery, 0 ); // Per Stratics?!?
			SetSkill( SkillName.Meditation, 0 ); // Per Stratics?!?
			SetSkill( SkillName.MagicResist, 0 ); // Per Stratics?!?
			SetSkill( SkillName.Tactics, 85.0, 100 ); // Stratics says 0?!?
			SetSkill( SkillName.Wrestling, 0 ); // Per Stratics?!?

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

		public void SpawnPixies( Mobile target )
		{
			Map map = this.Map;

			if ( map == null )
				return;

			int newPixies = Utility.RandomMinMax( 3, 6 );

			for ( int i = 0; i < newPixies; ++i )
			{
				Pixie pixie = new Pixie();

				pixie.Team = this.Team;
				pixie.FightMode = FightMode.Closest;

				bool validLocation = false;
				Point3D loc = this.Location;

				for ( int j = 0; !validLocation && j < 10; ++j )
				{
					int x = X + Utility.Random( 3 ) - 1;
					int y = Y + Utility.Random( 3 ) - 1;
					int z = map.GetAverageZ( x, y );

					if ( validLocation = map.CanFit( x, y, this.Z, 16, false, false ) )
						loc = new Point3D( x, y, Z );
					else if ( validLocation = map.CanFit( x, y, z, 16, false, false ) )
						loc = new Point3D( x, y, z );
				}

				pixie.MoveToWorld( loc, map );
				pixie.Combatant = target;
			}
		}

		public override void AlterDamageScalarFrom( Mobile caster, ref double scalar )
		{
			if ( 0.1 >= Utility.RandomDouble() )
				SpawnPixies( caster );
		}

		public override void OnGaveMeleeAttack( Mobile defender )
		{
			base.OnGaveMeleeAttack( defender );

			defender.Damage( Utility.Random( 20, 10 ), this );
			defender.Stam -= Utility.Random( 20, 10 );
			defender.Mana -= Utility.Random( 20, 10 );
		}

		public override void OnGotMeleeAttack( Mobile attacker )
		{
			base.OnGotMeleeAttack( attacker );

			if ( 0.1 >= Utility.RandomDouble() )
				SpawnPixies( attacker );
		}

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
