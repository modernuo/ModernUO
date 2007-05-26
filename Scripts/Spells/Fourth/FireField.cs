using System;
using System.Collections;
using Server.Targeting;
using Server.Network;
using Server.Misc;
using Server.Items;

namespace Server.Spells.Fourth
{
	public class FireFieldSpell : MagerySpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Fire Field", "In Flam Grav",
				215,
				9041,
				false,
				Reagent.BlackPearl,
				Reagent.SpidersSilk,
				Reagent.SulfurousAsh
			);

		public override SpellCircle Circle { get { return SpellCircle.Fourth; } }

		public FireFieldSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( IPoint3D p )
		{
			if ( !Caster.CanSee( p ) )
			{
				Caster.SendLocalizedMessage( 500237 ); // Target can not be seen.
			}
			else if ( SpellHelper.CheckTown( p, Caster ) && CheckSequence() )
			{
				SpellHelper.Turn( Caster, p );

				SpellHelper.GetSurfaceTop( ref p );

				int dx = Caster.Location.X - p.X;
				int dy = Caster.Location.Y - p.Y;
				int rx = (dx - dy) * 44;
				int ry = (dx + dy) * 44;

				bool eastToWest;

				if ( rx >= 0 && ry >= 0 )
				{
					eastToWest = false;
				}
				else if ( rx >= 0 )
				{
					eastToWest = true;
				}
				else if ( ry >= 0 )
				{
					eastToWest = true;
				}
				else
				{
					eastToWest = false;
				}

				Effects.PlaySound( p, Caster.Map, 0x20C );

				int itemID = eastToWest ? 0x398C : 0x3996;

				TimeSpan duration;

				if ( Core.AOS )
					duration = TimeSpan.FromSeconds( (15 + (Caster.Skills.Magery.Fixed / 5)) / 4 );
				else
					duration = TimeSpan.FromSeconds( 4.0 + (Caster.Skills[SkillName.Magery].Value * 0.5) );

				for ( int i = -2; i <= 2; ++i )
				{
					Point3D loc = new Point3D( eastToWest ? p.X + i : p.X, eastToWest ? p.Y : p.Y + i, p.Z );

					new FireFieldItem( itemID, loc, Caster, Caster.Map, duration, i );
				}
			}

			FinishSequence();
		}

		[DispellableField]
		public class FireFieldItem : Item
		{
			private Timer m_Timer;
			private DateTime m_End;
			private Mobile m_Caster;
			private int m_Damage;

			public override bool BlocksFit{ get{ return true; } }

			public FireFieldItem( int itemID, Point3D loc, Mobile caster, Map map, TimeSpan duration, int val )
				: this( itemID, loc, caster, map, duration, val, 2 )
			{
			}

			public FireFieldItem( int itemID, Point3D loc, Mobile caster, Map map, TimeSpan duration, int val, int damage ) : base( itemID )
			{
				bool canFit = SpellHelper.AdjustField( ref loc, map, 12, false );

				Visible = false;
				Movable = false;
				Light = LightType.Circle300;

				MoveToWorld( loc, map );

				m_Caster = caster;

				m_Damage = damage;

				m_End = DateTime.Now + duration;

				m_Timer = new InternalTimer( this, TimeSpan.FromSeconds( Math.Abs( val ) * 0.2 ), caster.InLOS( this ), canFit );
				m_Timer.Start();
			}

			public override void OnAfterDelete()
			{
				base.OnAfterDelete();

				if ( m_Timer != null )
					m_Timer.Stop();
			}

			public FireFieldItem( Serial serial ) : base( serial )
			{
			}

			public override void Serialize( GenericWriter writer )
			{
				base.Serialize( writer );

				writer.Write( (int) 2 ); // version

				writer.Write( m_Damage );
				writer.Write( m_Caster );
				writer.WriteDeltaTime( m_End );
			}

			public override void Deserialize( GenericReader reader )
			{
				base.Deserialize( reader );

				int version = reader.ReadInt();

				switch ( version )
				{
					case 2:
					{
						m_Damage = reader.ReadInt();
						goto case 1;
					}
					case 1:
					{
						m_Caster = reader.ReadMobile();

						goto case 0;
					}
					case 0:
					{
						m_End = reader.ReadDeltaTime();

						m_Timer = new InternalTimer( this, TimeSpan.Zero, true, true );
						m_Timer.Start();

						break;
					}
				}

				if( version < 2 )
					m_Damage = 2;
			}

			public override bool OnMoveOver( Mobile m )
			{
				if ( Visible && m_Caster != null && (!Core.AOS || m != m_Caster) && SpellHelper.ValidIndirectTarget( m_Caster, m ) && m_Caster.CanBeHarmful( m, false ) )
				{
					m_Caster.DoHarmful( m );

					int damage = m_Damage;

					if ( !Core.AOS && m.CheckSkill( SkillName.MagicResist, 0.0, 30.0 ) )
					{
						damage = 1;

						m.SendLocalizedMessage( 501783 ); // You feel yourself resisting magical energy.
					}

					AOS.Damage( m, m_Caster, damage, 0, 100, 0, 0, 0 );
					m.PlaySound( 0x208 );
				}

				return true;
			}

			private class InternalTimer : Timer
			{
				private FireFieldItem m_Item;
				private bool m_InLOS, m_CanFit;

				private static Queue m_Queue = new Queue();

				public InternalTimer( FireFieldItem item, TimeSpan delay, bool inLOS, bool canFit ) : base( delay, TimeSpan.FromSeconds( 1.0 ) )
				{
					m_Item = item;
					m_InLOS = inLOS;
					m_CanFit = canFit;

					Priority = TimerPriority.FiftyMS;
				}

				protected override void OnTick()
				{
					if ( m_Item.Deleted )
						return;

					if ( !m_Item.Visible )
					{
						if ( m_InLOS && m_CanFit )
							m_Item.Visible = true;
						else
							m_Item.Delete();

						if ( !m_Item.Deleted )
						{
							m_Item.ProcessDelta();
							Effects.SendLocationParticles( EffectItem.Create( m_Item.Location, m_Item.Map, EffectItem.DefaultDuration ), 0x376A, 9, 10, 5029 );
						}
					}
					else if ( DateTime.Now > m_Item.m_End )
					{
						m_Item.Delete();
						Stop();
					}
					else
					{
						Map map = m_Item.Map;
						Mobile caster = m_Item.m_Caster;

						if ( map != null && caster != null )
						{
							foreach ( Mobile m in m_Item.GetMobilesInRange( 0 ) )
							{
								if ( (m.Z + 16) > m_Item.Z && (m_Item.Z + 12) > m.Z && (!Core.AOS || m != caster) && SpellHelper.ValidIndirectTarget( caster, m ) && caster.CanBeHarmful( m, false ) )
									m_Queue.Enqueue( m );
							}

							while ( m_Queue.Count > 0 )
							{
								Mobile m = (Mobile)m_Queue.Dequeue();

								caster.DoHarmful( m );

								int damage = m_Item.m_Damage;

								if ( !Core.AOS && m.CheckSkill( SkillName.MagicResist, 0.0, 30.0 ) )
								{
									damage = 1;

									m.SendLocalizedMessage( 501783 ); // You feel yourself resisting magical energy.
								}

								AOS.Damage( m, caster, damage, 0, 100, 0, 0, 0 );
								m.PlaySound( 0x208 );
							}
						}
					}
				}
			}
		}

		private class InternalTarget : Target
		{
			private FireFieldSpell m_Owner;

			public InternalTarget( FireFieldSpell owner ) : base( 12, true, TargetFlags.None )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is IPoint3D )
					m_Owner.Target( (IPoint3D)o );
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}