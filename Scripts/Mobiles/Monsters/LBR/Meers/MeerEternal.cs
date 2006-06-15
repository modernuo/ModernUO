using System;
using System.Collections;
using Server;
using Server.Misc;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles
{
	[CorpseName( "a meer's corpse" )]
	public class MeerEternal : BaseCreature
	{
		[Constructable]
		public MeerEternal() : base( AIType.AI_Mage, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			Name = "a meer eternal";
			Body = 772;

			SetStr( 416, 505 );
			SetDex( 146, 165 );
			SetInt( 566, 655 );

			SetHits( 250, 303 );

			SetDamage( 11, 13 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 45, 55 );
			SetResistance( ResistanceType.Fire, 15, 25 );
			SetResistance( ResistanceType.Cold, 45, 55 );
			SetResistance( ResistanceType.Poison, 30, 40 );
			SetResistance( ResistanceType.Energy, 30, 40 );

			SetSkill( SkillName.EvalInt, 90.1, 100.0 );
			SetSkill( SkillName.Magery, 90.1, 100.0 );
			SetSkill( SkillName.Meditation, 90.1, 100.0 );
			SetSkill( SkillName.MagicResist, 150.5, 200.0 );
			SetSkill( SkillName.Tactics, 50.1, 70.0 );
			SetSkill( SkillName.Wrestling, 60.1, 80.0 );

			Fame = 18000;
			Karma = 18000;

			VirtualArmor = 34;

			m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds( Utility.RandomMinMax( 2, 5 ) );
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.FilthyRich, 2 );
			AddLoot( LootPack.MedScrolls, 2 );
			AddLoot( LootPack.HighScrolls, 2 );
		}

		public override bool AutoDispel{ get{ return true; } }
		public override bool BardImmune{ get{ return !Core.AOS; } }
		public override bool CanRummageCorpses{ get{ return true; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }
		public override int TreasureMapLevel{ get{ return Core.AOS ? 5 : 4; } }

		public override bool InitialInnocent{ get{ return true; } }

		public override int GetHurtSound()
		{
			return 0x167;
		}

		public override int GetDeathSound()
		{
			return 0xBC;
		}

		public override int GetAttackSound()
		{
			return 0x28B;
		}

		private DateTime m_NextAbilityTime;

		private void DoAreaLeech()
		{
			m_NextAbilityTime += TimeSpan.FromSeconds( 2.5 );

			this.Say( true, "Beware, mortals!  You have provoked my wrath!" );
			this.FixedParticles( 0x376A, 10, 10, 9537, 33, 0, EffectLayer.Waist );

			Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), new TimerCallback( DoAreaLeech_Finish ) );
		}

		private void DoAreaLeech_Finish()
		{
			ArrayList list = new ArrayList();

			foreach ( Mobile m in this.GetMobilesInRange( 6 ) )
			{
				if ( this.CanBeHarmful( m ) && this.IsEnemy( m ) )
					list.Add( m );
			}

			if ( list.Count == 0 )
			{
				this.Say( true, "Bah! You have escaped my grasp this time, mortal!" );
			}
			else
			{
				double scalar;

				if ( list.Count == 1 )
					scalar = 0.75;
				else if ( list.Count == 2 )
					scalar = 0.50;
				else
					scalar = 0.25;

				for ( int i = 0; i < list.Count; ++i )
				{
					Mobile m = (Mobile)list[i];

					int damage = (int)(m.Hits * scalar);

					damage += Utility.RandomMinMax( -5, 5 );

					if ( damage < 1 )
						damage = 1;

					m.MovingParticles( this, 0x36F4, 1, 0, false, false, 32, 0, 9535,    1, 0, (EffectLayer)255, 0x100 );
					m.MovingParticles( this, 0x0001, 1, 0, false,  true, 32, 0, 9535, 9536, 0, (EffectLayer)255, 0 );

					this.DoHarmful( m );
					this.Hits += AOS.Damage( m, this, damage, 100, 0, 0, 0, 0 );
				}

				this.Say( true, "If I cannot cleanse thy soul, I will destroy it!" );
			}
		}

		private void DoFocusedLeech( Mobile combatant, string message )
		{
			this.Say( true, message );

			Timer.DelayCall( TimeSpan.FromSeconds( 0.5 ), new TimerStateCallback( DoFocusedLeech_Stage1 ), combatant );
		}

		private void DoFocusedLeech_Stage1( object state )
		{
			Mobile combatant = (Mobile)state;

			if ( this.CanBeHarmful( combatant ) )
			{
				this.MovingParticles( combatant, 0x36FA, 1, 0, false, false, 1108, 0, 9533, 1,    0, (EffectLayer)255, 0x100 );
				this.MovingParticles( combatant, 0x0001, 1, 0, false,  true, 1108, 0, 9533, 9534, 0, (EffectLayer)255, 0 );
				this.PlaySound( 0x1FB );

				Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), new TimerStateCallback( DoFocusedLeech_Stage2 ), combatant );
			}
		}

		private void DoFocusedLeech_Stage2( object state )
		{
			Mobile combatant = (Mobile)state;

			if ( this.CanBeHarmful( combatant ) )
			{
				combatant.MovingParticles( this, 0x36F4, 1, 0, false, false, 32, 0, 9535, 1,    0, (EffectLayer)255, 0x100 );
				combatant.MovingParticles( this, 0x0001, 1, 0, false,  true, 32, 0, 9535, 9536, 0, (EffectLayer)255, 0 );

				this.PlaySound( 0x209 );
				this.DoHarmful( combatant );
				this.Hits += AOS.Damage( combatant, this, Utility.RandomMinMax( 30, 40 ) - (Core.AOS ? 0 : 10), 100, 0, 0, 0, 0 );
			}
		}

		public override void OnThink()
		{
			if ( DateTime.Now >= m_NextAbilityTime )
			{
				Mobile combatant = this.Combatant;

				if ( combatant != null && combatant.Map == this.Map && combatant.InRange( this, 12 ) )
				{
					m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds( Utility.RandomMinMax( 10, 15 ) );

					int ability = Utility.Random( 4 );

					switch ( ability )
					{
						case 0: DoFocusedLeech( combatant, "Thine essence will fill my withering body with strength!" ); break;
						case 1: DoFocusedLeech( combatant, "I rebuke thee, worm, and cleanse thy vile spirit of its tainted blood!" ); break;
						case 2: DoFocusedLeech( combatant, "I devour your life's essence to strengthen my resolve!" ); break;
						case 3: DoAreaLeech(); break;
							// TODO: Resurrect ability
					}
				}
			}

			base.OnThink();
		}

		public MeerEternal( Serial serial ) : base( serial )
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