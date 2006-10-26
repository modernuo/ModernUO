using System;
using Server;
using Server.Items;
using Server.Engines.CannedEvil;

namespace Server.Mobiles
{
	public class LordOaks : BaseChampion
	{
		private Mobile m_Queen;
		private bool m_SpawnedQueen;

		public override ChampionSkullType SkullType{ get{ return ChampionSkullType.Enlightenment; } }

		[Constructable]
		public LordOaks() : base( AIType.AI_Mage, FightMode.Evil )
		{
			Body = 175;
			Name = "Lord Oaks";

			SetStr( 403, 850 );
			SetDex( 101, 150 );
			SetInt( 503, 800 );

			SetHits( 3000 );
			SetStam( 202, 400 );

			SetDamage( 21, 33 );

			SetDamageType( ResistanceType.Physical, 75 );
			SetDamageType( ResistanceType.Fire, 25 );

			SetResistance( ResistanceType.Physical, 85, 90 );
			SetResistance( ResistanceType.Fire, 60, 70 );
			SetResistance( ResistanceType.Cold, 60, 70 );
			SetResistance( ResistanceType.Poison, 80, 90 );
			SetResistance( ResistanceType.Energy, 80, 90 );

			SetSkill( SkillName.Anatomy, 75.1, 100.0 );
			SetSkill( SkillName.EvalInt, 120.1, 130.0 );
			SetSkill( SkillName.Magery, 120.0 );
			SetSkill( SkillName.Meditation, 120.1, 130.0 );
			SetSkill( SkillName.MagicResist, 100.5, 150.0 );
			SetSkill( SkillName.Tactics, 100.0 );
			SetSkill( SkillName.Wrestling, 100.0 );

			Fame = 22500;
			Karma = 22500;

			VirtualArmor = 100;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.UltraRich, 5 );
		}
		
		public override bool AutoDispel{ get{ return true; } }

		public override bool BardImmune{ get{ return !Core.SE; } }
		public override bool Unprovokable{ get{ return Core.SE; } }
		public override bool Uncalmable{ get{ return Core.SE; } }
		public override OppositionGroup OppositionGroup
		{
			get{ return OppositionGroup.FeyAndUndead; }
		}
		
		public override Poison PoisonImmune{ get{ return Poison.Deadly; } }

		public void SpawnPixies( Mobile target )
		{
			Map map = this.Map;

			if ( map == null )
				return;

			this.Say( 1042154 ); // You shall never defeat me as long as I have my queen!

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

		public void CheckQueen()
		{
			if( this.Map == null )
				return;

			if ( !m_SpawnedQueen )
			{
				this.Say( 1042153 ); // Come forth my queen!

				m_Queen = new Silvani();

				((BaseCreature)m_Queen).Team = this.Team;

				m_Queen.MoveToWorld( this.Location, this.Map );

				m_SpawnedQueen = true;
			}
			else if ( m_Queen != null && m_Queen.Deleted )
			{
				m_Queen = null;
			}
		}

		public override void AlterDamageScalarFrom( Mobile caster, ref double scalar )
		{
			CheckQueen();

			if ( m_Queen != null )
			{
				scalar *= 0.1;

				if ( 0.1 >= Utility.RandomDouble() )
					SpawnPixies( caster );
			}
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

			CheckQueen();

			if ( m_Queen != null && 0.1 >= Utility.RandomDouble() )
				SpawnPixies( attacker );

			attacker.Damage( Utility.Random( 20, 10 ), this );
			attacker.Stam -= Utility.Random( 20, 10 );
			attacker.Mana -= Utility.Random( 20, 10 );
		}

		public LordOaks( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_Queen );
			writer.Write( m_SpawnedQueen );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Queen = reader.ReadMobile();
					m_SpawnedQueen = reader.ReadBool();

					break;
				}
			}
		}
	}
}