using System;
using System.Collections;
using Server;
using Server.Misc;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles
{
	[CorpseName( "a meer's corpse" )]
	public class MeerMage : BaseCreature
	{
		[Constructable]
		public MeerMage() : base( AIType.AI_Mage, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			Name = "a meer mage";
			Body = 770;

			SetStr( 171, 200 );
			SetDex( 126, 145 );
			SetInt( 276, 305 );

			SetHits( 103, 120 );

			SetDamage( 24, 26 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 45, 55 );
			SetResistance( ResistanceType.Fire, 15, 25 );
			SetResistance( ResistanceType.Cold, 50 );
			SetResistance( ResistanceType.Poison, 25, 35 );
			SetResistance( ResistanceType.Energy, 25, 35 );

			SetSkill( SkillName.EvalInt, 100.0 );
			SetSkill( SkillName.Magery, 70.1, 80.0 );
			SetSkill( SkillName.Meditation, 85.1, 95.0 );
			SetSkill( SkillName.MagicResist, 80.1, 100.0 );
			SetSkill( SkillName.Tactics, 70.1, 90.0 );
			SetSkill( SkillName.Wrestling, 60.1, 80.0 );

			Fame = 8000;
			Karma = 8000;

			VirtualArmor = 16;

			m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds( Utility.RandomMinMax( 2, 5 ) );
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.FilthyRich );
			AddLoot( LootPack.MedScrolls, 2 );
			// TODO: Daemon bone ...
		}

		public override bool AutoDispel{ get{ return true; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }
		public override bool CanRummageCorpses{ get{ return true; } }
		public override int TreasureMapLevel{ get{ return 3; } }

		public override bool InitialInnocent{ get{ return true; } }

		public override int GetHurtSound()
		{
			return 0x14D;
		}

		public override int GetDeathSound()
		{
			return 0x314;
		}

		public override int GetAttackSound()
		{
			return 0x75;
		}

		private DateTime m_NextAbilityTime;

		public override void OnThink()
		{
			if ( DateTime.Now >= m_NextAbilityTime )
			{
				Mobile combatant = this.Combatant;

				if ( combatant != null && combatant.Map == this.Map && combatant.InRange( this, 12 ) && IsEnemy( combatant ) && !UnderEffect( combatant ) )
				{
					m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds( Utility.RandomMinMax( 20, 30 ) );

					// TODO: Forest summon ability

					this.Say( true, "I call a plague of insects to sting your flesh!" );

					m_Table[combatant] = Timer.DelayCall( TimeSpan.FromSeconds( 0.5 ), TimeSpan.FromSeconds( 7.0 ), new TimerStateCallback( DoEffect ), new object[]{ combatant, 0 } );
				}
			}

			base.OnThink();
		}

		private static Hashtable m_Table = new Hashtable();

		public static bool UnderEffect( Mobile m )
		{
			return m_Table.Contains( m );
		}

		public static void StopEffect( Mobile m, bool message )
		{
			Timer t = (Timer)m_Table[m];

			if ( t != null )
			{
				if ( message )
					m.PublicOverheadMessage( Network.MessageType.Emote, m.SpeechHue, true, "* The open flame begins to scatter the swarm of insects *" );

				t.Stop();
				m_Table.Remove( m );
			}
		}

		public void DoEffect( object state )
		{
			object[] states = (object[])state;

			Mobile m = (Mobile)states[0];
			int count = (int)states[1];

			if ( !m.Alive )
			{
				StopEffect( m, false );
			}
			else
			{
				Torch torch = m.FindItemOnLayer( Layer.TwoHanded ) as Torch;

				if ( torch != null && torch.Burning )
				{
					StopEffect( m, true );
				}
				else
				{
					if ( (count % 4) == 0 )
					{
						m.LocalOverheadMessage( Network.MessageType.Emote, m.SpeechHue, true, "* The swarm of insects bites and stings your flesh! *" );
						m.NonlocalOverheadMessage( Network.MessageType.Emote, m.SpeechHue, true, String.Format( "* {0} is stung by a swarm of insects *", m.Name ) );
					}

					m.FixedParticles( 0x91C, 10, 180, 9539, EffectLayer.Waist );
					m.PlaySound( 0x00E );
					m.PlaySound( 0x1BC );

					AOS.Damage( m, this, Utility.RandomMinMax( 30, 40 ) - (Core.AOS ? 0 : 10), 100, 0, 0, 0, 0 );

					states[1] = count + 1;

					if ( !m.Alive )
						StopEffect( m, false );
				}
			}
		}

		public MeerMage( Serial serial ) : base( serial )
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