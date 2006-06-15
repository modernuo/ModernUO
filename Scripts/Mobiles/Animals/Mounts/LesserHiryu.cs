using System;
using System.Collections;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "a hiryu corpse" )]
	public class LesserHiryu : BaseMount
	{
		public override WeaponAbility GetWeaponAbility()
		{
			return WeaponAbility.Dismount;
		}

		public override bool StatLossAfterTame{ get{ return true; } }

		[Constructable]
		public LesserHiryu() : base( "a lesser hiryu", 243, 0x3E94, AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
			{
			if ( 0.05 > Utility.RandomDouble() )
				Hue = Utility.RandomList( 0x32, 0x37, 0x3C, 0x3D, 0x123, 0x294, 0x295, 0x47F, 0x482, 0x487, 0x48D, 0x490, 0x495, 0x55C, 0x8A0, 0x899, 0x89F ) | 0x8000;

			SetStr( 301, 410 );
			SetDex( 171, 270 );
			SetInt( 301, 325 );

			SetHits( 401, 600 );
			SetMana( 60 );

			SetDamage( 18, 23 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 45, 70 );
			SetResistance( ResistanceType.Fire, 60, 80 );
			SetResistance( ResistanceType.Cold, 5, 15 );
			SetResistance( ResistanceType.Poison, 30, 40 );
			SetResistance( ResistanceType.Energy, 30, 40 );

			SetSkill( SkillName.Anatomy, 75.1, 80.0 );
			SetSkill( SkillName.MagicResist, 85.1, 100.0 );
			SetSkill( SkillName.Tactics, 100.1, 110.0 );
			SetSkill( SkillName.Wrestling, 100.1, 120.0 );

			Fame = 18000;
			Karma = -18000;

			Tamable = true;
			ControlSlots = 3;
			MinTameSkill = 98.7;

			if ( Utility.RandomDouble() < .33 )
				PackItem( Engines.Plants.Seed.RandomBonsaiSeed() );
		}


		public override int GetAngerSound()
		{
			return 0x4FE;
		}

		public override int GetIdleSound()
		{
			return 0x4FD;
		}

		public override int GetAttackSound()
		{
			return 0x4FC;
		}

		public override int GetHurtSound()
		{
			return 0x4FF;
		}

		public override int GetDeathSound()
		{
			return 0x4FB;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.FilthyRich, 2 );
			AddLoot( LootPack.Gems, 4 );
		}

		public override double GetControlChance( Mobile m )
		{
			if ( m.Skills.Bushido.Value >= 90.0 )
				return 1.0;

			return base.GetControlChance( m );
		}

		public override int TreasureMapLevel{ get{ return 3; } }
		public override int Meat{ get{ return 16; } }
		public override int Hides{ get{ return 60; } }
		public override FoodType FavoriteFood{ get{ return FoodType.Meat; } }
		public override bool CanAngerOnTame { get { return true; } }

		public override void OnGaveMeleeAttack( Mobile defender )
		{
			base.OnGaveMeleeAttack( defender );

			if ( 0.1 > Utility.RandomDouble() )
			{
				/* Grasping Claw
				 * Start cliloc: 1070836
				 * Effect: Physical resistance -15% for 5 seconds
				 * End cliloc: 1070838
				 * Effect: Type: "3" - From: "0x57D4F5B" (player) - To: "0x0" - ItemId: "0x37B9" - ItemIdName: "glow" - FromLocation: "(1149 808, 32)" - ToLocation: "(1149 808, 32)" - Speed: "10" - Duration: "5" - FixedDirection: "True" - Explode: "False"
				 */

				ExpireTimer timer = (ExpireTimer)m_Table[defender];

				if ( timer != null )
				{
					timer.DoExpire();
					defender.SendLocalizedMessage( 1070837 ); // The creature lands another blow in your weakened state.
				}
				else
					defender.SendLocalizedMessage( 1070836 ); // The blow from the creature's claws has made you more susceptible to physical attacks.

				int effect = -(defender.PhysicalResistance * 15 / 100);

				ResistanceMod mod = new ResistanceMod( ResistanceType.Physical, effect );

				defender.FixedEffect( 0x37B9, 10, 5 );
				defender.AddResistanceMod( mod );

				timer = new ExpireTimer( defender, mod, TimeSpan.FromSeconds( 5.0 ) );
				timer.Start();
				m_Table[defender] = timer;
			}
		}

		private static Hashtable m_Table = new Hashtable();

		private class ExpireTimer : Timer
		{
			private Mobile m_Mobile;
			private ResistanceMod m_Mod;

			public ExpireTimer( Mobile m, ResistanceMod mod, TimeSpan delay ) : base( delay )
			{
				m_Mobile = m;
				m_Mod = mod;
				Priority = TimerPriority.TwoFiftyMS;
			}

			public void DoExpire()
			{
				m_Mobile.RemoveResistanceMod( m_Mod );
				Stop();
				m_Table.Remove( m_Mobile );
			}

			protected override void OnTick()
			{
				m_Mobile.SendLocalizedMessage( 1070838 ); // Your resistance to physical attacks has returned.
				DoExpire();
			}
		}

		public LesserHiryu( Serial serial ) : base( serial )
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
