using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
	public class PuzzlePad : Item
	{
		private PlayerMobile m_Stander;
		private bool m_Busy;

		[CommandProperty( AccessLevel.GameMaster )]
		public PlayerMobile Stander
		{
			get { return m_Stander; }
			set { m_Stander = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Busy
		{
			get { return m_Busy; }
			set { m_Busy = value; }
		}

		private InternalStandTimer m_Timer;

		[Constructable]
		public PuzzlePad() : base( 0x1822 )
		{
			m_Busy = false;
			m_Stander = null;
			m_Timer = null;

			Hue = 0x4C;
			Movable = false;
		}

		public override bool HandlesOnMovement { get { return true; } } // Tell the core that we implement OnMovement

		public override bool OnMoveOver( Mobile m )
		{
			if ( ( m != null ) && ( m is PlayerMobile ) )
			{
				if ( m_Stander == null )
				{
					m_Stander = (PlayerMobile) m;

					if ( m_Timer != null )
					{
						m_Timer.Stop();
					}

					m_Timer = new InternalStandTimer( this );

					m_Timer.Start();
				}
			}

			return base.OnMoveOver( m );
		}

		public PuzzlePad( Serial serial ) : base( serial )
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

		private class InternalStandTimer : Timer
		{
			private PuzzlePad m_Pad;

			public InternalStandTimer( PuzzlePad pad ) : base( TimeSpan.FromSeconds( 0.25 ), TimeSpan.FromSeconds( 0.25 ) )
			{
				m_Pad = pad;
				Priority = TimerPriority.FiftyMS;
			}

			private void Vacance()
			{
				if ( m_Pad != null )
				{
					m_Pad.Stander = null;
					m_Pad.Busy = false;
				}

				Stop();
			}

			protected override void OnTick()
			{
				if ( m_Pad != null && !m_Pad.Deleted && m_Pad.Stander != null )
				{
					if ( !m_Pad.Stander.Deleted && m_Pad.Stander.Alive && m_Pad.Stander.Location == m_Pad.Location )
					{
						m_Pad.Busy = true;
					}
					else
					{
						Vacance();
					}
				}
				else
				{
					Vacance();
				}
			}
		}
	}
}
