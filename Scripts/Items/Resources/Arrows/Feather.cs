using System;
using Server.Items;

namespace Server.Items
{
	public class Feather : Item, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( Amount == 1 ? "{0} feather" : "{0} feathers", Amount );
			}
		}

		int ICommodity.DescriptionNumber { get { return LabelNumber; } }

		public override double DefaultWeight
		{
			get { return 0.1; }
		}

		[Constructable]
		public Feather() : this( 1 )
		{
		}

		[Constructable]
		public Feather( int amount ) : base( 0x1BD1 )
		{
			Stackable = true;
			Amount = amount;
		}

		public Feather( Serial serial ) : base( serial )
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