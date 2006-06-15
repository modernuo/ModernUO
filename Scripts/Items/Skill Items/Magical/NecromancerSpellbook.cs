using System;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
	public class NecromancerSpellbook : Spellbook
	{
		public override SpellbookType SpellbookType{ get{ return SpellbookType.Necromancer; } }
		public override int BookOffset{ get{ return 100; } }
		public override int BookCount{ get{ return ((Core.SE) ? 17 : 16); } }

		[Constructable]
		public NecromancerSpellbook() : this( (ulong)0 )
		{
		}

		[Constructable]
		public NecromancerSpellbook( ulong content ) : base( content, 0x2253 )
		{
			Layer = (Core.ML ? Layer.OneHanded : Layer.Invalid);
		}

		public NecromancerSpellbook( Serial serial ) : base( serial )
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