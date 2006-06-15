using System;
using System.Collections;
using Server;

namespace Server.Items
{
	public class EffectItem : Item
	{
		private static ArrayList m_Free = new ArrayList(); // List of available EffectItems

		public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds( 5.0 );

		public static EffectItem Create( Point3D p, Map map, TimeSpan duration )
		{
			EffectItem item = null;

			for ( int i = m_Free.Count - 1; item == null && i >= 0; --i ) // We reuse new entries first so decay works better
			{
				EffectItem free = (EffectItem)m_Free[i];

				m_Free.RemoveAt( i );

				if ( !free.Deleted && free.Map == Map.Internal )
					item = free;
			}

			if ( item == null )
				item = new EffectItem();
			else
				item.ItemID = 1;

			item.MoveToWorld( p, map );
			item.BeginFree( duration );

			return item;
		}

		private EffectItem() : base( 1 ) // nodraw
		{
			Movable = false;
		}

		public void BeginFree( TimeSpan duration )
		{
			new FreeTimer( this, duration ).Start();
		}

		public override bool Decays
		{
			get
			{
				return true;
			}
		}

		public EffectItem( Serial serial ) : base( serial )
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

			Delete();
		}

		private class FreeTimer : Timer
		{
			private Item m_Item;

			public FreeTimer( Item item, TimeSpan delay ) : base( delay )
			{
				m_Item = item;
				Priority = TimerPriority.OneSecond;
			}

			protected override void OnTick()
			{
				m_Item.Internalize();

				m_Free.Add( m_Item );
			}
		}
	}
}