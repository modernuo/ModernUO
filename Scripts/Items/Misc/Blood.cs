using System;
using Server;

namespace Server.Items
{
	public class Blood : Item
	{
		[Constructable]
		public Blood() : this( 0x1645 )
		{
		}

		[Constructable]
		public Blood( int itemID ) : base( itemID )
		{
			Movable = false;

			new InternalTimer( this ).Start();
		}

		public Blood( Serial serial ) : base( serial )
		{
			new InternalTimer( this ).Start();
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

		private class InternalTimer : Timer
		{
			private Item m_Blood;

			public InternalTimer( Item blood ) : base( TimeSpan.FromSeconds( 5.0 ) )
			{
				Priority = TimerPriority.OneSecond;

				m_Blood = blood;
			}

			protected override void OnTick()
			{
				m_Blood.Delete();
			}
		}
	}
}