using System;
using Server;
using Server.Misc;

namespace Server.Mobiles
{
	public class NatureFury : BaseCreature
	{
		public override bool DeleteCorpseOnDeath { get { return Core.AOS; } }
		public override bool IsHouseSummonable { get { return true; } }

		public override double DispelDifficulty { get { return 125.0; } }
		public override double DispelFocus { get { return 90.0; } }

		public override bool BleedImmune { get { return true; } }
		public override Poison PoisonImmune { get { return Poison.Lethal; } }

		public override bool AlwaysMurderer { get { return true; } }

		private Mobile m_Target;

		public override Mobile ConstantFocus { get { return m_Target; } }

		public override void OnThink()
		{
			if( m_Target != null && (!m_Target.Alive || m_Target.Deleted || !InRange( m_Target, 24 ) || !CanSee( m_Target )) )
				m_Target = null;	//Go kill other stuffs

			base.OnThink();
		}

		[Constructable]
		public NatureFury( Mobile target )
			: base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			m_Target = target;
			Name = "a nature's fury";
			Body = 0x33;
			Hue = 0x4001;

			SetStr( 150 );
			SetDex( 150 );
			SetInt( 100 );

			SetHits( 80 );
			SetStam( 250 );
			SetMana( 0 );

			SetDamage( 5, 7 );

			SetDamageType( ResistanceType.Poison, 100 );

			SetResistance( ResistanceType.Physical, 90 );

			SetSkill( SkillName.Wrestling, 90.0 );
			SetSkill( SkillName.MagicResist, 70.0 );
			SetSkill( SkillName.Tactics, 100.0 );

			Fame = 0;
			Karma = 0;

			ControlSlots = 1;
		}

		[Constructable]
		public NatureFury()
			: this( null )	//Null makes it act like any other mob
		{
		}

		public override void MoveToWorld( Point3D loc, Map map )
		{
			base.MoveToWorld( loc, map );
			Timer.DelayCall( TimeSpan.Zero, DoEffects );
		}

		public void DoEffects()
		{
			FixedParticles( 0x91C, 10, 180, 0x2543, 0, 0, EffectLayer.Waist );
			PlaySound( 0xE );
			PlaySound( 0x1BC );

			if( Alive && !Deleted )
				Timer.DelayCall( TimeSpan.FromSeconds( 7.0 ), DoEffects );
		}

		public NatureFury( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			Delete();
		}
	}
}