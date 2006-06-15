using System;
using System.Collections;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "a kappa corpse" )]
	public class Kappa : BaseCreature
	{

		[Constructable]
		public Kappa() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "a kappa";
			Body = 240;

			SetStr( 186, 230 );
			SetDex( 51, 75 );
			SetInt( 41, 55 );

			SetHits( 151, 180 );

			SetDamage( 6, 12 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 35, 50 );
			SetResistance( ResistanceType.Fire, 35, 50 );
			SetResistance( ResistanceType.Cold, 25, 50 );
			SetResistance( ResistanceType.Poison, 35, 50 );
			SetResistance( ResistanceType.Energy, 20, 30 );

			SetSkill( SkillName.MagicResist, 60.1, 70.0 );
			SetSkill( SkillName.Tactics, 79.1, 89.0 );
			SetSkill( SkillName.Wrestling, 60.1, 70.0 );

			Fame = 1700;
			Karma = -1700;

			PackItem( new RawFishSteak( 3 ) );
			for( int i = 0; i < 2; i++ )
			{
				switch ( Utility.Random( 6 ) )
				{
					case 0: PackItem( new Gears() ); break;
					case 1: PackItem( new Hinge() ); break;
					case 2: PackItem( new Axle() ); break;
				}
			}


		}



		public override void GenerateLoot()
		{
			AddLoot( LootPack.Meager );
			AddLoot( LootPack.Average );
		}

		public override int Meat{ get{ return 1; } }
		public override int Hides{ get{ return 10; } }
		public override HideType HideType{ get{ return HideType.Spined; } }

		// TODO: Soul Sap
		/*
		 * Message: 1070820
		 * Spits pool of acid (blood, hue 0x3F), hits lost 6-10 per second/step
		 * Damage is resistable (physical)
		 * Acid last 10 seconds
		 */
		 
		public override int GetAngerSound()
		{
			return 0x50B;
		}

		public override int GetIdleSound()
		{
			return 0x50A;
		}

		public override int GetAttackSound()
		{
			return 0x509;
		}

		public override int GetHurtSound()
		{
			return 0x50C;
		}

		public override int GetDeathSound()
		{
			return 0x508;
		}

		public override void OnGaveMeleeAttack(Mobile defender)
 		{
			base.OnGaveMeleeAttack (defender);

			if ( 0.1 > Utility.RandomDouble() )
			{
				if ( !IsBeingDrained( defender ) )
				{
					defender.SendLocalizedMessage( 1070848 ); // You feel your life force being stolen away.
					BeginLifeDrain( defender, this );
				}
			}
		}

		private static Hashtable m_Table = new Hashtable();

		public static bool IsBeingDrained( Mobile m )
		{
			return m_Table.Contains( m );
		}

		public static void BeginLifeDrain( Mobile m, Mobile from )
		{
			Timer t = (Timer)m_Table[m];

			if ( t != null )
				t.Stop();

			t = new InternalTimer( from, m );
			m_Table[m] = t;

			t.Start();
		}

		public static void DrainLife( Mobile m, Mobile from )
		{
			if ( m.Alive )
			{
				int damageGiven = AOS.Damage( m, from, 5, 100, 0, 0, 0, 0 );

				from.Hits += damageGiven;
			}
			else
		{
				EndLifeDrain( m );
			}
		}

		public static void EndLifeDrain( Mobile m )
		{
			Timer t = (Timer)m_Table[m];

			if ( t != null )
				t.Stop();

			m_Table.Remove( m );

			m.SendLocalizedMessage( 1070849 ); // The drain on your life force is gone.
		}

		/*
		public override bool OnBeforeDeath()
		{
			SpillAcid( 1, 4, 10, 6, 10 );

			return base.OnBeforeDeath();
		}
		*/

		public Kappa( Serial serial ) : base( serial )
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

		private class InternalTimer : Timer
		{
			private Mobile m_From;
			private Mobile m_Mobile;
			private int m_Count;

			public InternalTimer( Mobile from, Mobile m ) : base( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 1.0 ) )
			{
				m_From = from;
				m_Mobile = m;
				Priority = TimerPriority.TwoFiftyMS;
			}

			protected override void OnTick()
			{
				DrainLife( m_Mobile, m_From );

				if ( ++m_Count == 5 )
					EndLifeDrain( m_Mobile );
			}
		}
	}
}
