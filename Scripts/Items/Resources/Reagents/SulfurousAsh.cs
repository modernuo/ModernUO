using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class SulfurousAsh : BaseReagent, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( "{0} sulfurous ash", Amount );
			}
		}

		[Constructable]
		public SulfurousAsh() : this( 1 )
		{
		}

		[Constructable]
		public SulfurousAsh( int amount ) : base( 0xF8C, amount )
		{
		}

		public SulfurousAsh( Serial serial ) : base( serial )
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