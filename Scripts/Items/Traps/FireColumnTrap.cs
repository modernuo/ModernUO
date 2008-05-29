using System;

namespace Server.Items
{
	public class FireColumnTrap : BaseTrap
	{
		[Constructable]
		public FireColumnTrap() : base( 0x1B71 )
		{
			m_MinDamage = 10;
			m_MaxDamage = 40;

			m_WarningFlame = true;
		}

		public override bool PassivelyTriggered{ get{ return true; } }
		public override TimeSpan PassiveTriggerDelay{ get{ return TimeSpan.FromSeconds( 2.0 ); } }
		public override int PassiveTriggerRange{ get{ return 3; } }
		public override TimeSpan ResetDelay{ get{ return TimeSpan.FromSeconds( 0.5 ); } }

		private int m_MinDamage;

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual int MinDamage
		{
			get { return m_MinDamage; }
			set { m_MinDamage = value; }
		}

		private int m_MaxDamage;

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual int MaxDamage
		{
			get { return m_MaxDamage; }
			set { m_MaxDamage = value; }
		}

		private bool m_WarningFlame;

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual bool WarningFlame
		{
			get { return m_WarningFlame; }
			set { m_WarningFlame = value; }
		}

		public override void OnTrigger( Mobile from )
		{
			if ( from.AccessLevel > AccessLevel.Player )
				return;

			if ( WarningFlame )
				DoEffect();

			if ( from.Alive && CheckRange( from.Location, 0 ) )
			{
				Spells.SpellHelper.Damage( TimeSpan.FromSeconds( 0.5 ), from, from, Utility.RandomMinMax( MinDamage, MaxDamage ), 0, 100, 0, 0, 0 );

				if ( !WarningFlame )
					DoEffect();
			}
		}

		private void DoEffect()
		{
			Effects.SendLocationParticles( EffectItem.Create( Location, Map, EffectItem.DefaultDuration ), 0x3709, 10, 30, 5052 );
			Effects.PlaySound( Location, Map, 0x225 );
		}

		public FireColumnTrap( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( m_WarningFlame );
			writer.Write( m_MinDamage );
			writer.Write( m_MaxDamage );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_WarningFlame = reader.ReadBool();
					m_MinDamage = reader.ReadInt();
					m_MaxDamage = reader.ReadInt();
					break;
				}
			}

			if ( version == 0 )
			{
				m_WarningFlame = true;
				m_MinDamage = 10;
				m_MaxDamage = 40;
			}
		}
	}
}