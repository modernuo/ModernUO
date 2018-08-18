using System;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
	public class BookOfNinjitsu : Spellbook
	{
		public override SpellbookType SpellbookType => SpellbookType.Ninja;
		public override int BookOffset => 500;
		public override int BookCount => 8;


		[Constructible]
		public BookOfNinjitsu() : this( (ulong)0xFF )
		{
		}

		[Constructible]
		public BookOfNinjitsu( ulong content ) : base( content, 0x23A0 )
		{
			Layer = (Core.ML ? Layer.OneHanded : Layer.Invalid);
		}

		public BookOfNinjitsu( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version == 0 && Core.ML )
				Layer = Layer.OneHanded;
		}
	}
}
