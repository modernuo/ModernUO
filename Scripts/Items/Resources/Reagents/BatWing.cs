using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class BatWing : BaseReagent, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( "{0} bat wing", Amount );
			}
		}

		[Constructable]
		public BatWing() : this( 1 )
		{
		}

		[Constructable]
		public BatWing( int amount ) : base( 0xF78, amount )
		{
		}

		public BatWing( Serial serial ) : base( serial )
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