using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace EvolutionPetSystem
{
	public abstract class BaseEvoDust: Item
	{
		public BaseEvoDust() : this( 1 )
		{
		}

		public BaseEvoDust( int amount ) : base( 0x26B8 )
		{
			Stackable = true;
			Weight = 0.0;
			Amount = amount;
			Name = "Evolution Dust";
			Hue = 1153;
		}

		public BaseEvoDust( Serial serial ) : base ( serial )
		{
		}

		public abstract BaseEvoDust NewDust();

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}
