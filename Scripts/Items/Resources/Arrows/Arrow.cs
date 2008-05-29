using System;

namespace Server.Items
{
	public class Arrow : Item, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( Amount == 1 ? "{0} arrow" : "{0} arrows", Amount );
			}
		}

		int ICommodity.DescriptionNumber { get { return LabelNumber; } }

		public override double DefaultWeight
		{
			get { return 0.1; }
		}

		[Constructable]
		public Arrow() : this( 1 )
		{
		}

		[Constructable]
		public Arrow( int amount ) : base( 0xF3F )
		{
			Stackable = true;
			Amount = amount;
		}

		public Arrow( Serial serial ) : base( serial )
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