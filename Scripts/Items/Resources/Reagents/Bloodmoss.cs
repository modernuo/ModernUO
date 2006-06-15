using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class Bloodmoss : BaseReagent, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( "{0} bloodmoss", Amount );
			}
		}

		[Constructable]
		public Bloodmoss() : this( 1 )
		{
		}

		[Constructable]
		public Bloodmoss( int amount ) : base( 0xF7B, amount )
		{
		}

		public Bloodmoss( Serial serial ) : base( serial )
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