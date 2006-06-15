using System;
using Server;

namespace Server.Items
{
	public class ElvenSpinningwheelEastAddon : BaseAddon, ISpinningWheel
	{
		public override BaseAddonDeed Deed{ get{ return new ElvenSpinningwheelEastDeed(); } }

		[Constructable]
		public ElvenSpinningwheelEastAddon()
		{
			AddComponent( new AddonComponent( 0x2DD9 ), 0, 0, 0 );
		}

		public ElvenSpinningwheelEastAddon( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}

		private Timer m_Timer;

		public override void OnComponentLoaded( AddonComponent c )
		{
			switch ( c.ItemID )
			{
				case 0x2E3D:
				case 0x101D:
				case 0x10A5: --c.ItemID; break;
			}
		}

		public bool Spinning{ get{ return m_Timer != null; } }

		public void BeginSpin( SpinCallback callback, Mobile from, int hue )
		{
			m_Timer = new SpinTimer( this, callback, from, hue );
			m_Timer.Start();

			foreach ( AddonComponent c in Components )
			{
				switch ( c.ItemID )
				{
					case 0x2DD9:
					case 0x101C:
					case 0x10A4: ++c.ItemID; break;
				}
			}
		}

		public void EndSpin( SpinCallback callback, Mobile from, int hue )
		{
			if ( m_Timer != null )
				m_Timer.Stop();

			m_Timer = null;

			foreach ( AddonComponent c in Components )
			{
				switch ( c.ItemID )
				{
					case 0x1016:
					case 0x101A:
					case 0x101D:
					case 0x10A5: --c.ItemID; break;
				}
			}

			if ( callback != null )
				callback( this, from, hue );
		}

		private class SpinTimer : Timer
		{
			private ElvenSpinningwheelEastAddon m_Wheel;
			private SpinCallback m_Callback;
			private Mobile m_From;
			private int m_Hue;

			public SpinTimer( ElvenSpinningwheelEastAddon wheel, SpinCallback callback, Mobile from, int hue ) : base( TimeSpan.FromSeconds( 3.0 ) )
			{
				m_Wheel = wheel;
				m_Callback = callback;
				m_From = from;
				m_Hue = hue;
				Priority = TimerPriority.TwoFiftyMS;
			}

			protected override void OnTick()
			{
				m_Wheel.EndSpin( m_Callback, m_From, m_Hue );
			}
		}
	}

	public class ElvenSpinningwheelEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ElvenSpinningwheelEastAddon(); } }
		public override int LabelNumber{ get{ return 1073393; } } // elven spinning wheel (east)

		[Constructable]
		public ElvenSpinningwheelEastDeed()
		{
		}

		public ElvenSpinningwheelEastDeed( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}