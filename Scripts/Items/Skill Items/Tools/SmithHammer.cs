using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	[FlippableAttribute( 0x13E3, 0x13E4 )]
	public class SmithHammer : BaseTool
	{
		public override CraftSystem CraftSystem{ get{ return DefBlacksmithy.CraftSystem; } }

		[Constructible]
		public SmithHammer() : base( 0x13E3 )
		{
			Weight = 8.0;
			Layer = Layer.OneHanded;
		}

		[Constructible]
		public SmithHammer( int uses ) : base( uses, 0x13E3 )
		{
			Weight = 8.0;
			Layer = Layer.OneHanded;
		}

		public SmithHammer( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}