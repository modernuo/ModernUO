using System;
using Server;

namespace Server.Items
{
	public class DragonsBlood : BaseReagent, ICommodity
	{
		int ICommodity.DescriptionNumber { get { return LabelNumber; } }
		bool ICommodity.IsDeedable { get { return Core.ML; } }

		[Constructable]
		public DragonsBlood()
			: this( 1 )
		{
		}

		[Constructable]
		public DragonsBlood( int amount )
			: base( 0x4077, amount )
		{
		}

		public DragonsBlood( Serial serial )
			: base( serial )
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
