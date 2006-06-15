using System;
using Server;
using Server.Misc;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
	[CorpseName( "a betrayer corpse" )]
	public class Betrayer : BaseCreature
	{
		private bool m_Stunning;

		[Constructable]
		public Betrayer() : base( AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "a betrayer";
			Body = 767;


			SetStr( 401, 500 );
			SetDex( 81, 100 );
			SetInt( 151, 200 );

			SetHits( 241, 300 );

			SetDamage( 16, 22 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 60, 70 );
			SetResistance( ResistanceType.Fire, 60, 70 );
			SetResistance( ResistanceType.Cold, 60, 70 );
			SetResistance( ResistanceType.Poison, 30, 40 );
			SetResistance( ResistanceType.Energy, 20, 30 );

			SetSkill( SkillName.Anatomy, 90.1, 100.0 );
			SetSkill( SkillName.EvalInt, 90.1, 100.0 );
			SetSkill( SkillName.Magery, 50.1, 100.0 );
			SetSkill( SkillName.Meditation, 90.1, 100.0 );
			SetSkill( SkillName.MagicResist, 120.1, 130.0 );
			SetSkill( SkillName.Tactics, 90.1, 100.0 );
			SetSkill( SkillName.Wrestling, 90.1, 100.0 );

			Fame = 15000;
			Karma = -15000;

			VirtualArmor = 65;
			SpeechHue = Utility.RandomDyedHue();

			PackItem( new PowerCrystal() );

			if ( 0.02 > Utility.RandomDouble() )
				PackItem( new BlackthornWelcomeBook() );

			m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds( Utility.RandomMinMax( 5, 30 ) );
		}

		public override int GetDeathSound()
		{
			return 0x423;
		}

		public override int GetAttackSound()
		{
			return 0x23B;
		}

		public override int GetHurtSound()
		{
			return 0x140;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.FilthyRich );
			AddLoot( LootPack.Rich );
			AddLoot( LootPack.Gems, 1 );
		}

		public override bool AlwaysMurderer{ get{ return true; } }
		public override bool BardImmune{ get{ return !Core.AOS; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }
		public override int Meat{ get{ return 1; } }
		public override int TreasureMapLevel{ get{ return 5; } }

		public override void OnGaveMeleeAttack( Mobile defender )
		{
			base.OnGaveMeleeAttack( defender );

			if ( !m_Stunning && 0.3 > Utility.RandomDouble() )
			{
				m_Stunning = true;

				defender.Animate( 21, 6, 1, true, false, 0 );
				this.PlaySound( 0xEE );
				defender.LocalOverheadMessage( MessageType.Regular, 0x3B2, false, "You have been stunned by a colossal blow!" );

				BaseWeapon weapon = this.Weapon as BaseWeapon;
				if ( weapon != null )
					weapon.OnHit( this, defender );

				if ( defender.Alive )
				{
					defender.Frozen = true;
					Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), new TimerStateCallback( Recover_Callback ), defender );
				}
			}
		}

		private void Recover_Callback( object state )
		{
			Mobile defender = state as Mobile;

			if ( defender != null )
			{
				defender.Frozen = false;
				defender.Combatant = null;
				defender.LocalOverheadMessage( MessageType.Regular, 0x3B2, false, "You recover your senses." );
			}

			m_Stunning = false;
		}

		private DateTime m_NextAbilityTime;

		public override void OnActionCombat()
		{
			Mobile combatant = Combatant;

			if ( DateTime.Now < m_NextAbilityTime || combatant == null || combatant.Deleted || combatant.Map != Map || !InRange( combatant, 3 ) || !CanBeHarmful( combatant ) || !InLOS( combatant ) )
				return;

			m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds( Utility.RandomMinMax( 5, 30 ) );

			if ( Utility.RandomBool() )
			{
				this.FixedParticles( 0x376A, 9, 32, 0x2539, EffectLayer.LeftHand );
				this.PlaySound( 0x1DE );

				foreach ( Mobile m in this.GetMobilesInRange( 2 ) )
				{
					if ( m != this && IsEnemy( m ) )
					{
						m.ApplyPoison( this, Poison.Deadly );
					}
				}
			}
		}

		public Betrayer( Serial serial ) : base( serial )
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