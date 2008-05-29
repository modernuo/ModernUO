using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class DaemonBlood : BaseReagent, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( "{0} daemon blood", Amount );
			}
		}

		int ICommodity.DescriptionNumber { get { return LabelNumber; } }

		[Constructable]
		public DaemonBlood() : this( 1 )
		{
		}

		[Constructable]
		public DaemonBlood( int amount ) : base( 0xF7D, amount )
		{
		}

		public DaemonBlood( Serial serial ) : base( serial )
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