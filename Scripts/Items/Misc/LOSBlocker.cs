using System;
using Server;
using Server.Network;

namespace Server.Items
{
	public class LOSBlocker : Item
	{
		public static void Initialize()
		{
			TileData.ItemTable[0x2199].Flags = TileFlag.Wall | TileFlag.NoShoot;
			TileData.ItemTable[0x2199].Height = 20;
		}

		public override string DefaultName
		{
			get { return "no line of sight"; }
		}

		[Constructable]
		public LOSBlocker() : base( 0x2199 )
		{
			Movable = false;
		}

		public LOSBlocker( Serial serial ) : base( serial )
		{
		}

		protected override Packet GetWorldPacketFor( NetState state ) {
			Mobile mob = state.Mobile;

			if ( mob != null && mob.AccessLevel >= AccessLevel.GameMaster ) {
				return new GMItemPacket( this );
			}

			return base.GetWorldPacketFor( state );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

		public sealed class GMItemPacket : Packet
		{
			public GMItemPacket( Item item ) : base( 0x1A )
			{
				this.EnsureCapacity( 20 );

				// 14 base length
				// +2 - Amount
				// +2 - Hue
				// +1 - Flags

				uint serial = (uint)item.Serial.Value;
				int itemID = 0x36FF;
				int amount = item.Amount;
				Point3D loc = item.Location;
				int x = loc.X;
				int y = loc.Y;
				int hue = item.Hue;
				int flags = item.GetPacketFlags();
				int direction = (int)item.Direction;

				if ( amount != 0 )
					serial |= 0x80000000;
				else
					serial &= 0x7FFFFFFF;

				m_Stream.Write( (uint) serial );
				m_Stream.Write( (short) (itemID & 0x7FFF) );

				if ( amount != 0 )
					m_Stream.Write( (short) amount );

				x &= 0x7FFF;

				if ( direction != 0 )
					x |= 0x8000;

				m_Stream.Write( (short) x );

				y &= 0x3FFF;

				if ( hue != 0 )
					y |= 0x8000;

				if ( flags != 0 )
					y |= 0x4000;

				m_Stream.Write( (short) y );

				if ( direction != 0 )
					m_Stream.Write( (byte) direction );

				m_Stream.Write( (sbyte) loc.Z );

				if ( hue != 0 )
					m_Stream.Write( (ushort) hue );

				if ( flags != 0 )
					m_Stream.Write( (byte) flags );
			}
		}
	}
}