using System;
using Server;

namespace Server.Items
{
	public class LieutenantOfTheBritannianRoyalGuard : BodySash
	{
		public override int LabelNumber{ get{ return 1094910; } } // Lieutenant of the Britannian Royal Guard [Replica]

		public override int InitMinHits{ get{ return 150; } }
		public override int InitMaxHits{ get{ return 150; } }

		public override bool CanFortify{ get{ return false; } }

		[Constructable]
		public LieutenantOfTheBritannianRoyalGuard()
		{
			Hue = 0x7B;

			Attributes.BonusInt = 5;
			Attributes.RegenMana = 2;
			Attributes.LowerRegCost = 10;
		}

		public LieutenantOfTheBritannianRoyalGuard( Serial serial ) : base( serial )
		{
		}
		
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
