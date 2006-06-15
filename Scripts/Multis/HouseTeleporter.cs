using System;
using System.Collections;
using Server;
using Server.Gumps;
using Server.Multis;
using Server.Targeting;
using System.Collections.Generic;
using Server.ContextMenus;

namespace Server.Items
{
	public class HouseTeleporter : Item, ISecurable
	{
		private Item m_Target;
		private SecureLevel m_Level;

		[CommandProperty( AccessLevel.GameMaster )]
		public Item Target
		{
			get{ return m_Target; }
			set{ m_Target = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public SecureLevel Level
		{
			get{ return m_Level; }
			set{ m_Level = value; }
		}

		[Constructable]
		public HouseTeleporter( int itemID ) : this( itemID, null )
		{
		}

		public HouseTeleporter( int itemID, Item target ) : base( itemID )
		{
			Movable = false;

			m_Level = SecureLevel.Anyone;

			m_Target = target;
		}

		public bool CheckAccess( Mobile m )
		{
			BaseHouse house = BaseHouse.FindHouseAt( this );

			if ( house != null && (house.Public ? house.IsBanned( m ) : !house.HasAccess( m )) )
				return false;

			return ( house != null && house.HasSecureAccess( m, m_Level ) );
		}

		public override bool OnMoveOver( Mobile m )
		{
			if ( m_Target != null && !m_Target.Deleted )
			{
				if ( CheckAccess( m ) )
				{
					if ( !m.Hidden || m.AccessLevel == AccessLevel.Player )
						new EffectTimer( Location, Map, 2023, 0x1F0, TimeSpan.FromSeconds( 0.4 ) ).Start();

					new DelayTimer( this, m ).Start();
				}
				else
				{
					m.SendLocalizedMessage( 1061637 ); // You are not allowed to access this.
				}
			}

			return true;
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );
			SetSecureLevelEntry.AddTo( from, this, list );
		}

		public HouseTeleporter( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( (int) m_Level );

			writer.Write( (Item) m_Target );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_Level = (SecureLevel)reader.ReadInt();
					goto case 0;
				}
				case 0:
				{
					m_Target = reader.ReadItem();

					if ( version < 0 )
						m_Level = SecureLevel.Anyone;

					break;
				}
			}
		}

		private class EffectTimer : Timer
		{
			private Point3D m_Location;
			private Map m_Map;
			private int m_EffectID;
			private int m_SoundID;

			public EffectTimer( Point3D p, Map map, int effectID, int soundID, TimeSpan delay ) : base( delay )
			{
				m_Location = p;
				m_Map = map;
				m_EffectID = effectID;
				m_SoundID = soundID;
			}

			protected override void OnTick()
			{
				Effects.SendLocationParticles( EffectItem.Create( m_Location, m_Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, m_EffectID, 0 );

				if ( m_SoundID != -1 )
					Effects.PlaySound( m_Location, m_Map, m_SoundID );
			}
		}

		private class DelayTimer : Timer
		{
			private HouseTeleporter m_Teleporter;
			private Mobile m_Mobile;

			public DelayTimer( HouseTeleporter tp, Mobile m ) : base( TimeSpan.FromSeconds( 1.0 ) )
			{
				m_Teleporter = tp;
				m_Mobile = m;
			}

			protected override void OnTick()
			{
				Item target = m_Teleporter.m_Target;

				if ( target != null && !target.Deleted )
				{
					Mobile m = m_Mobile;

					if ( m.Location == m_Teleporter.Location && m.Map == m_Teleporter.Map )
					{
						Point3D p = target.GetWorldTop();
						Map map = target.Map;

						Server.Mobiles.BaseCreature.TeleportPets( m, p, map );

						m.MoveToWorld( p, map );

						if ( !m.Hidden || m.AccessLevel == AccessLevel.Player )
						{
							Effects.PlaySound( target.Location, target.Map, 0x1FE );

							Effects.SendLocationParticles( EffectItem.Create( m_Teleporter.Location, m_Teleporter.Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 2023, 0 );
							Effects.SendLocationParticles( EffectItem.Create( target.Location, target.Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 5023, 0 );

							new EffectTimer( target.Location, target.Map, 2023, -1, TimeSpan.FromSeconds( 0.4 ) ).Start();
						}
					}
				}
			}
		}
	}
}