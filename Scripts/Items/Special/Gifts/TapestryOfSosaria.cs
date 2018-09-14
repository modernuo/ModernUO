using System.Collections.Generic;
using Server.Gumps;
using Server.Network;
using Server.Multis;
using Server.ContextMenus;

namespace Server.Items
{
	[FlippableAttribute( 0x234E, 0x234F )]
	public class TapestryOfSosaria : Item, ISecurable
	{
		public override int LabelNumber => 1062917; // The Tapestry of Sosaria

		[CommandProperty( AccessLevel.GameMaster )]
		public SecureLevel Level { get; set; }

		[Constructible]
		public TapestryOfSosaria() : base( 0x234E )
		{
			Weight = 1.0;
			LootType = LootType.Blessed;
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			SetSecureLevelEntry.AddTo( from, this, list );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.InRange( GetWorldLocation(), 2 ) )
			{
				from.CloseGump( typeof( InternalGump ) );
				from.SendGump( new InternalGump() );
			}
			else
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
			}
		}

		private class InternalGump : Gump
		{
			public InternalGump() : base( 50, 50 )
			{
				AddImage( 0, 0, 0x2C95 );
			}
		}

		public TapestryOfSosaria( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version

			writer.WriteEncodedInt( (int) Level );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();

			Level = (SecureLevel) reader.ReadEncodedInt();
		}
	}
}
