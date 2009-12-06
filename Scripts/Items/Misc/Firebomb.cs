using Server.Network;
using Server.Spells;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Items
{
	public class Firebomb : Item
	{
		private Timer m_Timer;
		private int m_Ticks = 0;
		private Mobile m_LitBy;

		[Constructable]
		public Firebomb() : this( 0x99B )
		{
		}

		[Constructable]
		public Firebomb( int itemID ) : base( itemID )
		{
			//Name = "a firebomb";
			Weight = 2.0;
			Hue = 1260;
		}

		public Firebomb( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !IsChildOf( from.Backpack ) )
			{
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
				return;
			}

			if ( Core.AOS && (from.Paralyzed || from.Frozen || (from.Spell != null && from.Spell.IsCasting)) )
			{
				//to prevent exploiting for pvp
				from.SendLocalizedMessage( 1075857 ); // You can not use that while paralyzed.
				return;
			} 

			if ( m_Timer == null )
			{
				m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 1 ), TimeSpan.FromSeconds( 1 ), new TimerCallback( OnFirebombTimerTick ) );
				m_LitBy = from;
				from.SendLocalizedMessage( 1060581 ); //You light the firebomb! Throw it now!
			}
			else
				from.SendLocalizedMessage( 1060582 ); //You've already lit it! Better throw it now!

			from.BeginTarget( 12, true, TargetFlags.None, new TargetCallback( OnFirebombTarget ) );
		}

		private void OnFirebombTimerTick()
		{
			if ( Deleted )
			{
				m_Timer.Stop();
				return;
			}

			if ( Map == Map.Internal && HeldBy == null )
				return;

			switch ( m_Ticks )
			{
				case 0:
				case 1:
				case 2:
				{
					++m_Ticks;

					if ( HeldBy != null )
						HeldBy.PublicOverheadMessage( MessageType.Regular, 957, false, m_Ticks.ToString() );
					else if ( RootParent == null )
						PublicOverheadMessage( MessageType.Regular, 957, false, m_Ticks.ToString() );
					else if ( RootParent is Mobile )
						((Mobile)RootParent).PublicOverheadMessage( MessageType.Regular, 957, false, m_Ticks.ToString() );

					break;
				}
				default:
				{
					if ( HeldBy != null )
						HeldBy.DropHolding();

					if ( RootParent is Mobile )
					{
						Mobile parent = (Mobile)RootParent;
						parent.SendLocalizedMessage( 1060583 ); //The firebomb explodes in your hand!
						AOS.Damage( parent, Utility.Random( 3 ) + 4, 0, 100, 0, 0, 0 );
					}
					else if ( RootParent == null )
					{
						List<Mobile> toDamage = new List<Mobile>();
						IPooledEnumerable eable = Map.GetMobilesInRange( Location, 1 );

						foreach ( Mobile m in eable )
						{
							toDamage.Add( m );
						}
						eable.Free();

						Mobile victim;
						for ( int i = 0; i < toDamage.Count; ++i )
						{
							victim = toDamage[i];

							if ( m_LitBy == null || (SpellHelper.ValidIndirectTarget( m_LitBy, victim ) && m_LitBy.CanBeHarmful( victim, false )) )
							{
								if ( m_LitBy != null )
									m_LitBy.DoHarmful( victim );

								AOS.Damage( victim, m_LitBy, Utility.Random( 3 ) + 4, 0, 100, 0, 0, 0 );
							}
						}
						(new FirebombField( m_LitBy, toDamage )).MoveToWorld( Location, Map );
					}

					m_Timer.Stop();
					Delete();
					break;
				}
			}
		}

		private void OnFirebombTarget( Mobile from, object obj )
		{
			if ( Deleted || Map == Map.Internal || !IsChildOf( from.Backpack ) )
				return;

			IPoint3D p = obj as IPoint3D;

			if ( p == null )
				return;

			SpellHelper.GetSurfaceTop( ref p );

			from.RevealingAction();

			IEntity to;

			if ( p is Mobile )
				to = (Mobile)p;
			else
				to = new Entity( Serial.Zero, new Point3D( p ), Map );

			Effects.SendMovingEffect( from, to, ItemID & 0x3FFF, 7, 0, false, false, Hue, 0 );

			Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), new TimerStateCallback( FirebombReposition_OnTick ), new object[]{ p, Map } );
			Internalize();
		}

		private void FirebombReposition_OnTick( object state )
		{
			if ( Deleted )
				return;

			object[] states = (object[])state;
			IPoint3D p = (IPoint3D)states[0];
			Map map = (Map)states[1];

			MoveToWorld( new Point3D( p ), map );
		}
	}

	public class FirebombField : Item
	{
		private List<Mobile> m_Burning;
		private Timer m_Timer;
		private Mobile m_LitBy;
		private DateTime m_Expire;

		public FirebombField( Mobile litBy, List<Mobile> toDamage ) : base( 0x376A )
		{
			Movable = false;
			m_LitBy = litBy;
			m_Expire = DateTime.Now + TimeSpan.FromSeconds( 10 );
			m_Burning = toDamage;
			m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 1.0 ), new TimerCallback( OnFirebombFieldTimerTick ) );
		}

		public FirebombField( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			//Don't serialize these...
		}

		public override void Deserialize( GenericReader reader )
		{
		}

		public override bool OnMoveOver( Mobile m )
		{
			if ( ItemID == 0x398C && m_LitBy == null || (SpellHelper.ValidIndirectTarget( m_LitBy, m ) && m_LitBy.CanBeHarmful( m, false )) )
			{
				if ( m_LitBy != null )
					m_LitBy.DoHarmful( m );

				AOS.Damage( m, m_LitBy, 2, 0, 100, 0, 0, 0 );
				m.PlaySound( 0x208 );

				if ( !m_Burning.Contains( m ) )
					m_Burning.Add( m );
			}

			return true;
		}

		private void OnFirebombFieldTimerTick()
		{
			if ( Deleted )
			{
				m_Timer.Stop();
				return;
			}

			if ( ItemID == 0x376A )
			{
				ItemID = 0x398C;
				return;
			}

			Mobile victim;
			for ( int i = 0; i < m_Burning.Count; )
			{
				victim = m_Burning[i];

				if ( victim.Location == Location && victim.Map == Map && (m_LitBy == null || ( SpellHelper.ValidIndirectTarget( m_LitBy, victim ) && m_LitBy.CanBeHarmful( victim, false ) )) )
				{
					if ( m_LitBy != null )
						m_LitBy.DoHarmful( victim );

					AOS.Damage( victim, m_LitBy, Utility.Random( 3 ) + 4, 0, 100, 0, 0, 0 );
					++i;
				}
				else
					m_Burning.RemoveAt( i );
			}

			if ( DateTime.Now >= m_Expire )
			{
				m_Timer.Stop();
				Delete();
			}
		}
	}
}
