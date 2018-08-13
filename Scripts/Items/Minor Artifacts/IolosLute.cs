using System;
using Server;

namespace Server.Items
{
	public class IolosLute : Lute
	{
		public override int LabelNumber => 1063479;

		public override int InitMinUses => 1600;
		public override int InitMaxUses => 1600;

		[Constructible]
		public IolosLute()
		{
			Hue = 0x47E;
			Slayer = SlayerName.Silver;
			//Slayer2 = SlayerName.DaemonDismissal;
			Slayer2 = SlayerName.Exorcism;
		}

		public IolosLute( Serial serial ) : base( serial )
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
