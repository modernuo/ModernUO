using System;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
	public class BookOfChivalry : Spellbook
	{
		public override SpellbookType SpellbookType => SpellbookType.Paladin;
		public override int BookOffset => 200;
		public override int BookCount => 10;

		[Constructible]
		public BookOfChivalry() : this( (ulong)0x3FF )
		{
		}

		[Constructible]
		public BookOfChivalry( ulong content ) : base( content, 0x2252 )
		{
			Layer = (Core.ML ? Layer.OneHanded : Layer.Invalid);
		}

		public BookOfChivalry( Serial serial ) : base( serial )
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

			if( version == 0 && Core.ML )
				Layer = Layer.OneHanded;
		}
	}
}
