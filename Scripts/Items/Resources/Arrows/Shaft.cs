using System;
using Server.Items;

namespace Server.Items
{
	public class Shaft : Item, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( Amount == 1 ? "{0} shaft" : "{0} shafts", Amount );
			}
		}

		int ICommodity.DescriptionNumber { get { return LabelNumber; } }

		public override double DefaultWeight
		{
			get { return 0.1; }
		}

		[Constructable]
		public Shaft() : this( 1 )
		{
		}

		[Constructable]
		public Shaft( int amount ) : base( 0x1BD4 )
		{
			Stackable = true;
			Amount = amount;
		}

		public Shaft( Serial serial ) : base( serial )
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