using System; 
using Server; 
using Server.Mobiles;

namespace Server.Items
{
	public class CompleteNecromancerSpellbook : NecromancerSpellbook
	{
		[Constructable]
		public CompleteNecromancerSpellbook() : base( (ulong) 0x1FFFF )
		{
		}

		public CompleteNecromancerSpellbook( Serial serial ) : base( serial )
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
