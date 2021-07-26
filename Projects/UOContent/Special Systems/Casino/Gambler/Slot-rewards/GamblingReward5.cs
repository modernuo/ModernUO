using System;
using Server.Items;

namespace Server.Items
{
	public class GamblingReward5 : Item
	{
		[Constructible]
		public GamblingReward5() : base( 10325 )
		{
			Name = "a gambling reward";
			Movable = true;
			Weight = 8.0;
		}
		public GamblingReward5( Serial serial ) : base( serial )
		{
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
