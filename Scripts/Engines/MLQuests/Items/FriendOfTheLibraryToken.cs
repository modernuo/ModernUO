using System;
using Server;

namespace Server.Items
{
	public class FriendOfTheLibraryToken : Item
	{
		public override int LabelNumber{ get{ return 1073136; } } // Friend of the Library Token (allows donations to be made)

		[Constructable]
		public FriendOfTheLibraryToken() : base( 0x2F58 )
		{
			Layer = Layer.Talisman;
			Hue = 0x28A;
		}

		public FriendOfTheLibraryToken( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // Version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
