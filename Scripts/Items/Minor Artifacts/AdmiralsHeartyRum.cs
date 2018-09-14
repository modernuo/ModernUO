namespace Server.Items
{
	public class AdmiralsHeartyRum : BeverageBottle
	{
		public override int LabelNumber => 1063477;

		[Constructible]
		public AdmiralsHeartyRum() : base( BeverageType.Ale )
		{
			Hue = 0x66C;
		}

		public AdmiralsHeartyRum( Serial serial ) : base( serial )
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
