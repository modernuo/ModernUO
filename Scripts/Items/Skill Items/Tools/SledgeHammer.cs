using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	[FlipableAttribute( 0xFB5, 0xFB4 )]
	public class SledgeHammer : BaseTool
	{
		public override CraftSystem CraftSystem{ get{ return DefBlacksmithy.CraftSystem; } }

		[Constructable]
		public SledgeHammer() : base( 0xFB5 )
		{
			Layer = Layer.OneHanded;
		}

		[Constructable]
		public SledgeHammer( int uses ) : base( uses, 0xFB5 )
		{
			Layer = Layer.OneHanded;
		}

		public SledgeHammer( Serial serial ) : base( serial )
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
