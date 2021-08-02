using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace EvolutionPetSystem
{
	public class SpiderDust : BaseEvoDust
	{
		[Constructible]
		public SpiderDust() : this( 1 )
		{
		}

		[Constructible]
		public SpiderDust( int amount ) : base( amount )
		{
			Amount = amount;
			Name = "Spider Dust";
			Hue = 1153;
		}

        public SpiderDust(Serial serial)
            : base(serial)
		{
		}

		public override BaseEvoDust NewDust()
		{
            return new SpiderDust();
		}

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
