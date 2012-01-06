using System; 
using Server; 
using Server.Mobiles;

namespace Server.Items
{
	public class HallowedSpellbook : Spellbook
	{
		public override int LabelNumber { get { return 1077620; } } // Hallowed Spellbook

		[Constructable]
		public HallowedSpellbook() : base()
		{
			LootType = LootType.Blessed;
			Content = 0x3FFFFFFFF;

			Slayer = SlayerName.Silver;
		}

		public HallowedSpellbook( Serial serial ) : base( serial )
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
