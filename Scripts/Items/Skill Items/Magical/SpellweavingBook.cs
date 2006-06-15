using System;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
	public class SpellweavingBook : Spellbook
	{
		public override SpellbookType SpellbookType{ get{ return SpellbookType.Arcanist; } }
		public override int BookOffset{ get{ return 600; } }
		public override int BookCount{ get{ return 16; } }

		[Constructable]
		public SpellweavingBook() : this( (ulong)0 )
		{
		}

		[Constructable]
		public SpellweavingBook( ulong content ) : base( content, 0x2D50 )
		{
			Hue = 0x8A2;

			Layer = Layer.OneHanded;
		}

		public SpellweavingBook( Serial serial ) : base( serial )
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