using System;
using System.Collections;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
	[CorpseName( "a minion's corpse" )]
	public class ExodusMinion : BaseCreature
	{
		private bool m_FieldActive;
		public bool FieldActive{ get{ return m_FieldActive; } }
		public bool CanUseField{ get{ return Hits >= HitsMax * 9 / 10; } } // TODO: an OSI bug prevents to verify this

		public override bool IsScaredOfScaryThings{ get{ return false; } }
		public override bool IsScaryToPets{ get{ return true; } }

		[Constructable]
		public ExodusMinion() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "exodus minion";
			Body = 0x2F5;

			SetStr( 851, 950 );
			SetDex( 71, 80 );
			SetInt( 61, 90 );

			SetHits( 511, 570 );

			SetDamage( 16, 22 );

			SetResistance( ResistanceType.Physical, 60, 70 );
			SetResistance( ResistanceType.Fire, 40, 50 );
			SetResistance( ResistanceType.Cold, 15, 25);
			SetResistance( ResistanceType.Poison, 15, 25 );
			SetResistance( ResistanceType.Energy, 15, 25 );

			SetSkill( SkillName.MagicResist, 90.1, 100.0 );
			SetSkill( SkillName.Tactics, 90.1, 100.0 );
			SetSkill( SkillName.Wrestling, 90.1, 100.0 );

			Fame = 18000;
			Karma = -18000;
			VirtualArmor = 65;

			PackItem( new PowerCrystal() );
			PackItem( new ArcaneGem() );
			PackItem( new ClockworkAssembly() );

			switch( Utility.Random( 3 ) )
			{
				case 0: PackItem( new PowerCrystal() ); break;
				case 1: PackItem( new ArcaneGem() ); break;
				case 2: PackItem( new ClockworkAssembly() ); break;
			}

			m_FieldActive = CanUseField;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.Average );
			AddLoot( LootPack.Rich );
		}

		public override bool AutoDispel{ get{ return true; } }
		public override bool BardImmune{ get{ return !Core.AOS; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }

		public override int GetIdleSound()
		{
			return 0x218;
		}

		public override int GetAngerSound()
		{
			return 0x26C;
		}

		public override int GetDeathSound()
		{
			return 0x211;
		}

		public override int GetAttackSound()
		{
			return 0x232;
		}

		public override int GetHurtSound()
		{
			return 0x140;
		}

		public override void AlterMeleeDamageFrom( Mobile from, ref int damage )
		{
			if ( m_FieldActive )
				damage = 0; // no melee damage when the field is up
		}

		public override void AlterSpellDamageFrom( Mobile from, ref int damage )
		{
			if ( !m_FieldActive )
				damage = 0; // no spell damage when the field is down
		}

		public override void OnDamagedBySpell( Mobile from )
		{
			if( from != null && from.Alive && 0.4 > Utility.RandomDouble() )
			{
				SendEBolt( from );
			}

			if ( !m_FieldActive )
			{
				// should there be an effect when spells nullifying is on?
				this.FixedParticles( 0, 10, 0, 0x2522, EffectLayer.Waist );
			}
			else if ( m_FieldActive && !CanUseField )
			{
				m_FieldActive = false;

				// TODO: message and effect when field turns down; cannot be verified on OSI due to a bug
				this.FixedParticles( 0x3735, 1, 30, 0x251F, EffectLayer.Waist );
			}
		}

		public override void OnGotMeleeAttack( Mobile attacker )
		{
			base.OnGotMeleeAttack( attacker );

			if ( m_FieldActive )
			{
				this.FixedParticles( 0x376A, 20, 10, 0x2530, EffectLayer.Waist );

				PlaySound( 0x2F4 );

				attacker.SendAsciiMessage( "Your weapon cannot penetrate the creature's magical barrier" );
			}

			if( attacker != null && attacker.Alive && attacker.Weapon is BaseRanged && 0.4 > Utility.RandomDouble() )
			{
				SendEBolt( attacker );
			}
		}

		public override void OnThink()
		{
			base.OnThink();

			// TODO: an OSI bug prevents to verify if the field can regenerate or not
			if ( !m_FieldActive && !IsHurt() )
				m_FieldActive = true;
		}

		public override bool Move( Direction d )
		{
			bool move = base.Move( d );

			if ( move && m_FieldActive && this.Combatant != null )
				this.FixedParticles( 0, 10, 0, 0x2530, EffectLayer.Waist );

			return move;
		}

		public void SendEBolt( Mobile to )
		{
			this.MovingParticles( to, 0x379F, 7, 0, false, true, 0xBE3, 0xFCB, 0x211 );
			to.PlaySound( 0x229 );
			this.DoHarmful( to );
			AOS.Damage( to, this, 50, 0, 0, 0, 0, 100 );
		}

		public ExodusMinion( Serial serial ) : base( serial )
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

			m_FieldActive = CanUseField;

			if( this.Name == "Exodus Minion" )
				this.Name = "exodus minion";
		}
	}
}