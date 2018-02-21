using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class MandrakeRoot : BaseReagent, ICommodity
	{
		int ICommodity.DescriptionNumber { get { return LabelNumber; } }
		bool ICommodity.IsDeedable { get { return true; } }

		[Constructible]
		public MandrakeRoot() : this( 1 )
		{
		}

		[Constructible]
		public MandrakeRoot( int amount ) : base( 0xF86, amount )
		{
		}

		public MandrakeRoot( Serial serial ) : base( serial )
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