using System;
using Server.Items;

namespace Server.Items
{
	[FlipableAttribute( 0x11EA, 0x11EB )]
	public class Sand : Item, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( Amount == 1 ? "{0} sand" : "{0} sand", Amount );
			}
		}

		int ICommodity.DescriptionNumber { get { return LabelNumber; } }

		public override int LabelNumber{ get{ return 1044626; } } // sand

		[Constructable]
		public Sand() : this( 1 )
		{
		}

		[Constructable]
		public Sand( int amount ) : base( 0x11EA )
		{
			Stackable = false;
			Weight = 1.0;
		}

		public Sand( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version == 0 && this.Name == "sand" )
				this.Name = null;
		}
	}
}