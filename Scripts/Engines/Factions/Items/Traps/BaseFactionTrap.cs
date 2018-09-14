using System;
using Server.Items;
using Server.Network;

namespace Server.Factions
{
	public enum AllowedPlacing
	{
		Everywhere,

		AnyFactionTown,
		ControlledFactionTown,
		FactionStronghold
	}

	public abstract class BaseFactionTrap : BaseTrap
	{
		private Faction m_Faction;
		private Mobile m_Placer;
		private DateTime m_TimeOfPlacement;

		private Timer m_Concealing;

		[CommandProperty( AccessLevel.GameMaster )]
		public Faction Faction
		{
			get => m_Faction;
			set => m_Faction = value;
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Placer
		{
			get => m_Placer;
			set => m_Placer = value;
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime TimeOfPlacement
		{
			get => m_TimeOfPlacement;
			set => m_TimeOfPlacement = value;
		}

		public virtual int EffectSound => 0;

		public virtual int SilverFromDisarm => 100;

		public virtual int MessageHue => 0;

		public virtual int AttackMessage => 0;
		public virtual int DisarmMessage => 0;

		public virtual AllowedPlacing AllowedPlacing => AllowedPlacing.Everywhere;

		public virtual TimeSpan ConcealPeriod => TimeSpan.FromMinutes( 1.0 );

		public virtual TimeSpan DecayPeriod
		{
			get
			{
				if ( Core.AOS )
					return TimeSpan.FromDays( 1.0 );

				return TimeSpan.MaxValue; // no decay
			}
		}

		public override void OnTrigger( Mobile from )
		{
			if ( !IsEnemy( from ) )
				return;

			Conceal();

			DoVisibleEffect();
			Effects.PlaySound( Location, Map, EffectSound );
			DoAttackEffect( from );

			int silverToAward = ( from.Alive ? 20 : 40 );

			if ( silverToAward > 0 && m_Placer != null && m_Faction != null )
			{
				PlayerState victimState = PlayerState.Find( from );

				if ( victimState != null && victimState.CanGiveSilverTo( m_Placer ) && victimState.KillPoints > 0 )
				{
					int silverGiven = m_Faction.AwardSilver( m_Placer, silverToAward );

					if ( silverGiven > 0 )
					{
						// TODO: Get real message
						if ( from.Alive )
							m_Placer.SendMessage( "You have earned {0} silver pieces because {1} fell for your trap.", silverGiven, from.Name );
						else
							m_Placer.SendLocalizedMessage( 1042736, $"{silverGiven} silver\t{@from.Name}"); // You have earned ~1_SILVER_AMOUNT~ pieces for vanquishing ~2_PLAYER_NAME~!
					}

					victimState.OnGivenSilverTo( m_Placer );
				}
			}

			from.LocalOverheadMessage( MessageType.Regular, MessageHue, AttackMessage );
		}

		public abstract void DoVisibleEffect();
		public abstract void DoAttackEffect( Mobile m );

		public virtual int IsValidLocation()
		{
			return IsValidLocation( GetWorldLocation(), Map );
		}

		public virtual int IsValidLocation( Point3D p, Map m )
		{
			if ( m == null )
				return 502956; // You cannot place a trap on that.

			if ( Core.ML )
			{
				foreach( Item item in m.GetItemsInRange( p, 0 ) )
				{
					if ( item is BaseFactionTrap trap && trap.Faction == Faction )
						return 1075263; // There is already a trap belonging to your faction at this location.;
				}
			}

			switch( AllowedPlacing )
			{
				case AllowedPlacing.FactionStronghold:
				{
					StrongholdRegion region = (StrongholdRegion) Region.Find( p, m ).GetRegion( typeof( StrongholdRegion ) );

					if ( region != null && region.Faction == m_Faction )
						return 0;

					return 1010355; // This trap can only be placed in your stronghold
				}
				case AllowedPlacing.AnyFactionTown:
				{
					Town town = Town.FromRegion( Region.Find( p, m ) );

					if ( town != null )
						return 0;

					return 1010356; // This trap can only be placed in a faction town
				}
				case AllowedPlacing.ControlledFactionTown:
				{
					Town town = Town.FromRegion( Region.Find( p, m ) );

					if ( town != null && town.Owner == m_Faction )
						return 0;

					return 1010357; // This trap can only be placed in a town your faction controls
				}
			}

			return 0;
		}

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			base.OnMovement( m, oldLocation );

			if ( !CheckDecay() && CheckRange( m.Location, oldLocation, 6 ) )
			{
				if ( Faction.Find( m ) != null && ((m.Skills[SkillName.DetectHidden].Value - 80.0) / 20.0) > Utility.RandomDouble() )
					PrivateOverheadLocalizedMessage( m, 1010154, MessageHue, "", "" ); // [Faction Trap]
			}
		}

		public void PrivateOverheadLocalizedMessage( Mobile to, int number, int hue, string name, string args )
		{
			NetState ns = to?.NetState;

			ns?.Send( new MessageLocalized( Serial, ItemID, MessageType.Regular, hue, 3, number, name, args ) );
		}

		public BaseFactionTrap( Faction f, Mobile m, int itemID ) : base( itemID )
		{
			Visible = false;

			m_Faction = f;
			m_TimeOfPlacement = DateTime.UtcNow;
			m_Placer = m;
		}

		public BaseFactionTrap( Serial serial ) : base( serial )
		{
		}

		public virtual bool CheckDecay()
		{
			TimeSpan decayPeriod = DecayPeriod;

			if ( decayPeriod == TimeSpan.MaxValue )
				return false;

			if ( (m_TimeOfPlacement + decayPeriod) < DateTime.UtcNow )
			{
				Timer.DelayCall( TimeSpan.Zero, Delete );
				return true;
			}

			return false;
		}

		public virtual void BeginConceal()
		{
			m_Concealing?.Stop();

			m_Concealing = Timer.DelayCall( ConcealPeriod, Conceal );
		}

		public virtual void Conceal()
		{
			m_Concealing?.Stop();

			m_Concealing = null;

			if ( !Deleted )
				Visible = false;
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			Faction.WriteReference( writer, m_Faction );
			writer.Write( (Mobile) m_Placer );
			writer.Write( (DateTime) m_TimeOfPlacement );

			if ( Visible )
				BeginConceal();
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Faction = Faction.ReadReference( reader );
			m_Placer = reader.ReadMobile();
			m_TimeOfPlacement = reader.ReadDateTime();

			if ( Visible )
				BeginConceal();

			CheckDecay();
		}

		public override void OnDelete()
		{
			if ( m_Faction != null && m_Faction.Traps.Contains( this ) )
				m_Faction.Traps.Remove( this );

			base.OnDelete();
		}

		public virtual bool IsEnemy( Mobile mob )
		{
			if ( mob.Hidden && mob.AccessLevel > AccessLevel.Player )
				return false;

			if ( !mob.Alive || mob.IsDeadBondedPet )
				return false;

			Faction faction = Faction.Find( mob, true );

			if ( faction == null && mob is BaseFactionGuard guard )
				faction = guard.Faction;

			if ( faction == null )
				return false;

			return ( faction != m_Faction );
		}
	}
}
