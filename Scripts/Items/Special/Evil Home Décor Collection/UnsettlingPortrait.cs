using System;
using Server;
using Server.Network;

namespace Server.Items
{
	[Flipable( 0x2A65, 0x2A67 )]
	public class UnsettlingPortraitComponent : AddonComponent
	{
		public override int LabelNumber { get { return 1074480; } } // Unsettling portrait

		private Timer m_Timer;

		public UnsettlingPortraitComponent() : base( 0x2A65 )
		{
			m_Timer = Timer.DelayCall( TimeSpan.FromMinutes( 3 ), TimeSpan.FromMinutes( 3 ), new TimerCallback( ChangeDirection ) );
		}

		public UnsettlingPortraitComponent( Serial serial ) : base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( Utility.InRange( Location, from.Location, 2 ) )
				Effects.PlaySound( Location, Map, Utility.RandomMinMax( 0x567, 0x568 ) );
			else
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if ( m_Timer != null )
				m_Timer.Stop();
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

			m_Timer = Timer.DelayCall( TimeSpan.FromMinutes( 3 ), TimeSpan.FromMinutes( 3 ), new TimerCallback( ChangeDirection ) );
		}

		private void ChangeDirection()
		{
			if ( ItemID == 0x2A65 )
				ItemID += 1;
			else if ( ItemID == 0x2A66 )
				ItemID -= 1;
			else if ( ItemID == 0x2A67 )
				ItemID += 1;
			else if ( ItemID == 0x2A68 )
				ItemID -= 1;
		}
	}

	public class UnsettlingPortraitAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new UnsettlingPortraitDeed(); } }

		[Constructable]
		public UnsettlingPortraitAddon() : base()
		{
			AddComponent( new UnsettlingPortraitComponent(), 0, 0, 0 );
		}

		public UnsettlingPortraitAddon( Serial serial ) : base( serial )
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

	public class UnsettlingPortraitDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new UnsettlingPortraitAddon(); } }
		public override int LabelNumber { get { return 1074480; } } // Unsettling portrait

		[Constructable]
		public UnsettlingPortraitDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public UnsettlingPortraitDeed( Serial serial ) : base( serial )
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
